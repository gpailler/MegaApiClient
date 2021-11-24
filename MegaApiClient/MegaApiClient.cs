namespace CG.Web.MegaApiClient
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using System.IO;
  using System.Linq;
  using System.Security.Cryptography;
  using System.Text.RegularExpressions;
  using System.Threading;
#if !NET40
  using System.Threading.Tasks;
#endif
  using CG.Web.MegaApiClient.Cryptography;
  using Medo.Security.Cryptography;
  using Newtonsoft.Json;
  using Newtonsoft.Json.Linq;
  using Serialization;

  public partial class MegaApiClient : IMegaApiClient
  {
    private static readonly Uri s_baseApiUri = new Uri("https://g.api.mega.co.nz/cs");
    private static readonly Uri s_baseUri = new Uri("https://mega.nz");

    private readonly Options _options;
    private readonly IWebClient _webClient;

    private readonly object _apiRequestLocker = new object();

    private Node _trashNode;
    private string _sessionId;
    private byte[] _masterKey;
    private uint _sequenceIndex = (uint)(uint.MaxValue * new Random().NextDouble());
    private bool _authenticatedLogin;

    #region Constructors

    /// <summary>
    /// Instantiate a new <see cref="MegaApiClient" /> object with default <see cref="Options"/> and default <see cref="IWebClient"/>
    /// </summary>
    public MegaApiClient()
        : this(new Options(), new WebClient())
    {
    }

    /// <summary>
    /// Instantiate a new <see cref="MegaApiClient" /> object with custom <see cref="Options" /> and default <see cref="IWebClient"/>
    /// </summary>
    public MegaApiClient(Options options)
        : this(options, new WebClient())
    {
    }

    /// <summary>
    /// Instantiate a new <see cref="MegaApiClient" /> object with default <see cref="Options" /> and custom <see cref="IWebClient"/>
    /// </summary>
    public MegaApiClient(IWebClient webClient)
        : this(new Options(), webClient)
    {
    }

    /// <summary>
    /// Instantiate a new <see cref="MegaApiClient" /> object with custom <see cref="Options"/> and custom <see cref="IWebClient" />
    /// </summary>
    public MegaApiClient(Options options, IWebClient webClient)
    {
      _options = options ?? throw new ArgumentNullException(nameof(options));
      _webClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
      _webClient.BufferSize = options.BufferSize;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Generate authentication informations and store them in a serializable object to allow persistence
    /// </summary>
    /// <param name="email">email</param>
    /// <param name="password">password</param>
    /// <param name="mfaKey"></param>
    /// <returns><see cref="AuthInfos" /> object containing encrypted data</returns>
    /// <exception cref="ArgumentNullException">email or password is null</exception>
    public AuthInfos GenerateAuthInfos(string email, string password, string mfaKey = null)
    {
      if (string.IsNullOrEmpty(email))
      {
        throw new ArgumentNullException("email");
      }

      if (string.IsNullOrEmpty(password))
      {
        throw new ArgumentNullException("password");
      }

      // Prelogin to retrieve account version
      var preLoginRequest = new PreLoginRequest(email);
      var preLoginResponse = Request<PreLoginResponse>(preLoginRequest);

      if (preLoginResponse.Version == 2 && !string.IsNullOrEmpty(preLoginResponse.Salt))
      {
        // Mega uses a new way to hash password based on a salt sent by Mega during prelogin
        var saltBytes = preLoginResponse.Salt.FromBase64();
        var passwordBytes = password.ToBytesPassword();
        const int Iterations = 100000;

        var derivedKeyBytes = new byte[32];
        using (var hmac = new HMACSHA512())
        {
          var pbkdf2 = new Pbkdf2(hmac, passwordBytes, saltBytes, Iterations);
          derivedKeyBytes = pbkdf2.GetBytes(derivedKeyBytes.Length);
        }

        // Derived key contains master key (0-16) and password hash (16-32)
        if (!string.IsNullOrEmpty(mfaKey))
        {
          return new AuthInfos(
            email,
            derivedKeyBytes.Skip(16).ToArray().ToBase64(),
            derivedKeyBytes.Take(16).ToArray(),
            mfaKey);
        }

        return new AuthInfos(
          email,
          derivedKeyBytes.Skip(16).ToArray().ToBase64(),
          derivedKeyBytes.Take(16).ToArray());
      }
      else if (preLoginResponse.Version == 1)
      {
        // Retrieve password as UTF8 byte array
        var passwordBytes = password.ToBytesPassword();

        // Encrypt password to use password as key for the hash
        var passwordAesKey = PrepareKey(passwordBytes);

        // Hash email and password to decrypt master key on Mega servers
        var hash = GenerateHash(email.ToLowerInvariant(), passwordAesKey);
        if (!string.IsNullOrEmpty(mfaKey))
        {
          return new AuthInfos(email, hash, passwordAesKey, mfaKey);
        }

        return new AuthInfos(email, hash, passwordAesKey);
      }
      else
      {
        throw new NotSupportedException("Version of account not supported");
      }
    }

    public event EventHandler<ApiRequestFailedEventArgs> ApiRequestFailed;

    public bool IsLoggedIn => _sessionId != null;

    /// <summary>
    /// Login to Mega.co.nz service using email/password credentials
    /// </summary>
    /// <param name="email">email</param>
    /// <param name="password">password</param>
    /// <exception cref="ApiException">Service is not available or credentials are invalid</exception>
    /// <exception cref="ArgumentNullException">email or password is null</exception>
    /// <exception cref="NotSupportedException">Already logged in</exception>
    public LogonSessionToken Login(string email, string password)
    {
      return Login(GenerateAuthInfos(email, password));
    }

    /// <summary>
    /// Login to Mega.co.nz service using email/password credentials
    /// </summary>
    /// <param name="email">email</param>
    /// <param name="password">password</param>
    /// <param name="mfaKey"></param>
    /// <exception cref="ApiException">Service is not available or credentials are invalid</exception>
    /// <exception cref="ArgumentNullException">email or password is null</exception>
    /// <exception cref="NotSupportedException">Already logged in</exception>
    public LogonSessionToken Login(string email, string password, string mfaKey)
    {
      return Login(GenerateAuthInfos(email, password, mfaKey));
    }

    /// <summary>
    /// Login to Mega.co.nz service using hashed credentials
    /// </summary>
    /// <param name="authInfos">Authentication informations generated by <see cref="GenerateAuthInfos"/> method</param>
    /// <exception cref="ApiException">Service is not available or authInfos is invalid</exception>
    /// <exception cref="ArgumentNullException">authInfos is null</exception>
    /// <exception cref="NotSupportedException">Already logged in</exception>
    public LogonSessionToken Login(AuthInfos authInfos)
    {
      if (authInfos == null)
      {
        throw new ArgumentNullException("authInfos");
      }

      EnsureLoggedOut();
      _authenticatedLogin = true;

      // Request Mega Api
      LoginRequest request;
      if (!string.IsNullOrEmpty(authInfos.MFAKey))
      {
        request = new LoginRequest(authInfos.Email, authInfos.Hash, authInfos.MFAKey);
      }
      else
      {
        request = new LoginRequest(authInfos.Email, authInfos.Hash);
      }

      var response = Request<LoginResponse>(request);

      // Decrypt master key using our password key
      var cryptedMasterKey = response.MasterKey.FromBase64();
      _masterKey = Crypto.DecryptKey(cryptedMasterKey, authInfos.PasswordAesKey);

      // Decrypt RSA private key using decrypted master key
      var cryptedRsaPrivateKey = response.PrivateKey.FromBase64();
      var rsaPrivateKeyComponents = Crypto.GetRsaPrivateKeyComponents(cryptedRsaPrivateKey, _masterKey);

      // Decrypt session id
      var encryptedSid = response.SessionId.FromBase64();
      var sid = Crypto.RsaDecrypt(encryptedSid.FromMPINumber(), rsaPrivateKeyComponents[0], rsaPrivateKeyComponents[1], rsaPrivateKeyComponents[2]);

      // Session id contains only the first 43 bytes
      _sessionId = sid.Take(43).ToArray().ToBase64();

      return new LogonSessionToken(_sessionId, _masterKey);
    }

    public void Login(LogonSessionToken logonSessionToken)
    {
      EnsureLoggedOut();
      _authenticatedLogin = true;

      _sessionId = logonSessionToken.SessionId;
      _masterKey = logonSessionToken.MasterKey;
    }

    /// <summary>
    /// Login anonymously to Mega.co.nz service
    /// </summary>
    /// <exception cref="ApiException">Throws if service is not available</exception>
    public void Login()
    {
      LoginAnonymous();
    }

    /// <summary>
    /// Login anonymously to Mega.co.nz service
    /// </summary>
    /// <exception cref="ApiException">Throws if service is not available</exception>
    public void LoginAnonymous()
    {
      EnsureLoggedOut();
      _authenticatedLogin = false;

      var random = new Random();

      // Generate random master key
      _masterKey = new byte[16];
      random.NextBytes(_masterKey);

      // Generate a random password used to encrypt the master key
      var passwordAesKey = new byte[16];
      random.NextBytes(passwordAesKey);

      // Generate a random session challenge
      var sessionChallenge = new byte[16];
      random.NextBytes(sessionChallenge);

      var encryptedMasterKey = Crypto.EncryptAes(_masterKey, passwordAesKey);

      // Encrypt the session challenge with our generated master key
      var encryptedSessionChallenge = Crypto.EncryptAes(sessionChallenge, _masterKey);
      var encryptedSession = new byte[32];
      Array.Copy(sessionChallenge, 0, encryptedSession, 0, 16);
      Array.Copy(encryptedSessionChallenge, 0, encryptedSession, 16, encryptedSessionChallenge.Length);

      // Request Mega Api to obtain a temporary user handle
      var request = new AnonymousLoginRequest(encryptedMasterKey.ToBase64(), encryptedSession.ToBase64());
      var userHandle = Request(request);

      // Request Mega Api to retrieve our temporary session id
      var request2 = new LoginRequest(userHandle, null);
      var response2 = Request<LoginResponse>(request2);

      _sessionId = response2.TemporarySessionId;
    }

    /// <summary>
    /// Logout from Mega.co.nz service
    /// </summary>
    /// <exception cref="NotSupportedException">Not logged in</exception>
    public void Logout()
    {
      EnsureLoggedIn();

      if (_authenticatedLogin == true)
      {
        Request(new LogoutRequest());
      }

      // Reset values retrieved by Login methods
      _masterKey = null;
      _sessionId = null;
    }

    /// <summary>
    /// Retrieve recovery key
    /// </summary>
    /// <exception cref="NotSupportedException">Not logged in</exception>
    public string GetRecoveryKey()
    {
      EnsureLoggedIn();

      if (!_authenticatedLogin)
      {
        throw new NotSupportedException("Anonymous login is not supported");
      }

      return _masterKey.ToBase64();
    }

    /// <summary>
    /// Retrieve account (quota) information
    /// </summary>
    /// <returns>An object containing account information</returns>
    /// <exception cref="NotSupportedException">Not logged in</exception>
    /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
    public IAccountInformation GetAccountInformation()
    {
      EnsureLoggedIn();

      var request = new AccountInformationRequest();
      return Request<AccountInformationResponse>(request);
    }

    /// <summary>
    /// Retrieve session history
    /// </summary>
    /// <returns>A collection of sessions</returns>
    /// <exception cref="NotSupportedException">Not logged in</exception>
    /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
    public IEnumerable<ISession> GetSessionsHistory()
    {
      EnsureLoggedIn();

      var request = new SessionHistoryRequest();
      return Request<SessionHistoryResponse>(request);
    }

    /// <summary>
    /// Retrieve all filesystem nodes
    /// </summary>
    /// <returns>Flat representation of all the filesystem nodes</returns>
    /// <exception cref="NotSupportedException">Not logged in</exception>
    /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
    public IEnumerable<INode> GetNodes()
    {
      EnsureLoggedIn();

      var request = new GetNodesRequest();
      var response = Request<GetNodesResponse>(request, _masterKey);

      var nodes = response.Nodes;
      if (_trashNode == null)
      {
        _trashNode = nodes.First(n => n.Type == NodeType.Trash);
      }

      return nodes.Distinct().OfType<INode>();
    }

    /// <summary>
    /// Retrieve children nodes of a parent node
    /// </summary>
    /// <returns>Flat representation of children nodes</returns>
    /// <exception cref="NotSupportedException">Not logged in</exception>
    /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
    /// <exception cref="ArgumentNullException">Parent node is null</exception>
    public IEnumerable<INode> GetNodes(INode parent)
    {
      if (parent == null)
      {
        throw new ArgumentNullException("parent");
      }

      return GetNodes().Where(n => n.ParentId == parent.Id);
    }

    /// <summary>
    /// Delete a node from the filesytem
    /// </summary>
    /// <remarks>
    /// You can only delete <see cref="NodeType.Directory" /> or <see cref="NodeType.File" /> node
    /// </remarks>
    /// <param name="node">Node to delete</param>
    /// <param name="moveToTrash">Moved to trash if true, Permanently deleted if false</param>
    /// <exception cref="NotSupportedException">Not logged in</exception>
    /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
    /// <exception cref="ArgumentNullException">node is null</exception>
    /// <exception cref="ArgumentException">node is not a directory or a file</exception>
    public void Delete(INode node, bool moveToTrash = true)
    {
      if (node == null)
      {
        throw new ArgumentNullException("node");
      }

      if (node.Type != NodeType.Directory && node.Type != NodeType.File)
      {
        throw new ArgumentException("Invalid node type");
      }

      EnsureLoggedIn();

      if (moveToTrash)
      {
        Move(node, _trashNode);
      }
      else
      {
        Request(new DeleteRequest(node));
      }
    }

    /// <summary>
    /// Create a folder on the filesytem
    /// </summary>
    /// <param name="name">Folder name</param>
    /// <param name="parent">Parent node to attach created folder</param>
    /// <exception cref="NotSupportedException">Not logged in</exception>
    /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
    /// <exception cref="ArgumentNullException">name or parent is null</exception>
    /// <exception cref="ArgumentException">parent is not valid (all types are allowed expect <see cref="NodeType.File" />)</exception>
    public INode CreateFolder(string name, INode parent)
    {
      if (string.IsNullOrEmpty(name))
      {
        throw new ArgumentNullException("name");
      }

      if (parent == null)
      {
        throw new ArgumentNullException("parent");
      }

      if (parent.Type == NodeType.File)
      {
        throw new ArgumentException("Invalid parent node");
      }

      EnsureLoggedIn();

      var key = Crypto.CreateAesKey();
      var attributes = Crypto.EncryptAttributes(new Attributes(name), key);
      var encryptedKey = Crypto.EncryptAes(key, _masterKey);

      var request = CreateNodeRequest.CreateFolderNodeRequest(parent, attributes.ToBase64(), encryptedKey.ToBase64(), key);
      var response = Request<GetNodesResponse>(request, _masterKey);
      return response.Nodes[0];
    }

    /// <summary>
    /// Retrieve an url to download specified node
    /// </summary>
    /// <param name="node">Node to retrieve the download link (only <see cref="NodeType.File" /> or <see cref="NodeType.Directory" /> can be downloaded)</param>
    /// <returns>Download link to retrieve the node with associated key</returns>
    /// <exception cref="NotSupportedException">Not logged in</exception>
    /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
    /// <exception cref="ArgumentNullException">node is null</exception>
    /// <exception cref="ArgumentException">node is not valid (only <see cref="NodeType.File" /> or <see cref="NodeType.Directory" /> can be downloaded)</exception>
    public Uri GetDownloadLink(INode node)
    {
      if (node == null)
      {
        throw new ArgumentNullException("node");
      }

      if (node.Type != NodeType.File && node.Type != NodeType.Directory)
      {
        throw new ArgumentException("Invalid node");
      }

      EnsureLoggedIn();

      if (node.Type == NodeType.Directory)
      {
        // Request an export share on the node or we will receive an AccessDenied
        Request(new ShareNodeRequest(node, _masterKey, GetNodes()));

        node = GetNodes().First(x => x.Equals(node));
      }

      if (!(node is INodeCrypto nodeCrypto))
      {
        throw new ArgumentException("node must implement INodeCrypto");
      }

      var request = new GetDownloadLinkRequest(node);
      var response = Request<string>(request);

      return new Uri(s_baseUri, string.Format(
          "/{0}/{1}#{2}",
          node.Type == NodeType.Directory ? "folder" : "file",
          response,
          node.Type == NodeType.Directory ? nodeCrypto.SharedKey.ToBase64() : nodeCrypto.FullKey.ToBase64()));
    }

    /// <summary>
    /// Download a specified node and save it to the specified file
    /// </summary>
    /// <param name="node">Node to download (only <see cref="NodeType.File" /> can be downloaded)</param>
    /// <param name="outputFile">File to save the node to</param>
    /// <param name="cancellationToken">CancellationToken used to cancel the action</param>
    /// <exception cref="NotSupportedException">Not logged in</exception>
    /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
    /// <exception cref="ArgumentNullException">node or outputFile is null</exception>
    /// <exception cref="ArgumentException">node is not valid (only <see cref="NodeType.File" /> can be downloaded)</exception>
    /// <exception cref="DownloadException">Checksum is invalid. Downloaded data are corrupted</exception>
    public void DownloadFile(INode node, string outputFile, CancellationToken? cancellationToken = null)
    {
      if (node == null)
      {
        throw new ArgumentNullException("node");
      }

      if (string.IsNullOrEmpty(outputFile))
      {
        throw new ArgumentNullException("outputFile");
      }

      using (var stream = Download(node, cancellationToken))
      {
        SaveStream(stream, outputFile);
      }
    }

    /// <summary>
    /// Download a specified Uri from Mega and save it to the specified file
    /// </summary>
    /// <param name="uri">Uri to download</param>
    /// <param name="outputFile">File to save the Uri to</param>
    /// <param name="cancellationToken">CancellationToken used to cancel the action</param>
    /// <exception cref="NotSupportedException">Not logged in</exception>
    /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
    /// <exception cref="ArgumentNullException">uri or outputFile is null</exception>
    /// <exception cref="ArgumentException">Uri is not valid (id and key are required)</exception>
    /// <exception cref="DownloadException">Checksum is invalid. Downloaded data are corrupted</exception>
    public void DownloadFile(Uri uri, string outputFile, CancellationToken? cancellationToken = null)
    {
      if (uri == null)
      {
        throw new ArgumentNullException("uri");
      }

      if (string.IsNullOrEmpty(outputFile))
      {
        throw new ArgumentNullException("outputFile");
      }

      using (var stream = Download(uri, cancellationToken))
      {
        SaveStream(stream, outputFile);
      }
    }

    /// <summary>
    /// Retrieve a Stream to download and decrypt the specified node
    /// </summary>
    /// <param name="node">Node to download (only <see cref="NodeType.File" /> can be downloaded)</param>
    /// <param name="cancellationToken">CancellationToken used to cancel the action</param>
    /// <exception cref="NotSupportedException">Not logged in</exception>
    /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
    /// <exception cref="ArgumentNullException">node or outputFile is null</exception>
    /// <exception cref="ArgumentException">node is not valid (only <see cref="NodeType.File" /> can be downloaded)</exception>
    /// <exception cref="DownloadException">Checksum is invalid. Downloaded data are corrupted</exception>
    public Stream Download(INode node, CancellationToken? cancellationToken = null)
    {
      if (node == null)
      {
        throw new ArgumentNullException("node");
      }

      if (node.Type != NodeType.File)
      {
        throw new ArgumentException("Invalid node");
      }

      if (!(node is INodeCrypto nodeCrypto))
      {
        throw new ArgumentException("node must implement INodeCrypto");
      }

      EnsureLoggedIn();

      // Retrieve download URL
      var downloadRequest = node is PublicNode publicNode && publicNode.ParentId == null ? (RequestBase)new DownloadUrlRequestFromId(node.Id) : new DownloadUrlRequest(node);
      var downloadResponse = Request<DownloadUrlResponse>(downloadRequest);

      Stream dataStream = new BufferedStream(_webClient.GetRequestRaw(new Uri(downloadResponse.Url)));

      Stream resultStream = new MegaAesCtrStreamDecrypter(dataStream, downloadResponse.Size, nodeCrypto.Key, nodeCrypto.Iv, nodeCrypto.MetaMac);

      if (cancellationToken.HasValue)
      {
        resultStream = new CancellableStream(resultStream, cancellationToken.Value);
      }

      return resultStream;
    }

    /// <summary>
    /// Retrieve a Stream to download and decrypt the specified Uri
    /// </summary>
    /// <param name="uri">Uri to download</param>
    /// <param name="cancellationToken">CancellationToken used to cancel the action</param>
    /// <exception cref="NotSupportedException">Not logged in</exception>
    /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
    /// <exception cref="ArgumentNullException">uri is null</exception>
    /// <exception cref="ArgumentException">Uri is not valid (id and key are required)</exception>
    /// <exception cref="DownloadException">Checksum is invalid. Downloaded data are corrupted</exception>
    public Stream Download(Uri uri, CancellationToken? cancellationToken = null)
    {
      if (uri == null)
      {
        throw new ArgumentNullException("uri");
      }

      EnsureLoggedIn();

      GetPartsFromUri(uri, out var id, out var iv, out var metaMac, out var key);

      // Retrieve download URL
      var downloadRequest = new DownloadUrlRequestFromId(id);
      var downloadResponse = Request<DownloadUrlResponse>(downloadRequest);

      Stream dataStream = new BufferedStream(_webClient.GetRequestRaw(new Uri(downloadResponse.Url)));

      Stream resultStream = new MegaAesCtrStreamDecrypter(dataStream, downloadResponse.Size, key, iv, metaMac);

      if (cancellationToken.HasValue)
      {
        resultStream = new CancellableStream(resultStream, cancellationToken.Value);
      }

      return resultStream;
    }

    /// <summary>
    /// Retrieve public properties of a file from a specified Uri
    /// </summary>
    /// <param name="uri">Uri to retrive properties</param>
    /// <exception cref="NotSupportedException">Not logged in</exception>
    /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
    /// <exception cref="ArgumentNullException">uri is null</exception>
    /// <exception cref="ArgumentException">Uri is not valid (id and key are required)</exception>
    public INode GetNodeFromLink(Uri uri)
    {
      if (uri == null)
      {
        throw new ArgumentNullException("uri");
      }

      EnsureLoggedIn();

      GetPartsFromUri(uri, out var id, out var iv, out var metaMac, out var key);

      // Retrieve attributes
      var downloadRequest = new DownloadUrlRequestFromId(id);
      var downloadResponse = Request<DownloadUrlResponse>(downloadRequest);

      return new PublicNode(new Node(id, downloadResponse, key, iv, metaMac), null);
    }

    /// <summary>
    /// Retrieve list of nodes from a specified Uri
    /// </summary>
    /// <param name="uri">Uri</param>
    /// <exception cref="NotSupportedException">Not logged in</exception>
    /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
    /// <exception cref="ArgumentNullException">uri is null</exception>
    /// <exception cref="ArgumentException">Uri is not valid (id and key are required)</exception>
    public IEnumerable<INode> GetNodesFromLink(Uri uri)
    {
      if (uri == null)
      {
        throw new ArgumentNullException("uri");
      }

      EnsureLoggedIn();

      GetPartsFromUri(uri, out var shareId, out _, out _, out var key);

      // Retrieve attributes
      var getNodesRequest = new GetNodesRequest(shareId);
      var getNodesResponse = Request<GetNodesResponse>(getNodesRequest, key);

      return getNodesResponse.Nodes.Select(x => new PublicNode(x, shareId)).OfType<INode>();
    }

    /// <summary>
    /// Upload a file on Mega.co.nz and attach created node to selected parent
    /// </summary>
    /// <param name="filename">File to upload</param>
    /// <param name="parent">Node to attach the uploaded file (all types except <see cref="NodeType.File" /> are supported)</param>
    /// <param name="cancellationToken">CancellationToken used to cancel the action</param>
    /// <returns>Created node</returns>
    /// <exception cref="NotSupportedException">Not logged in</exception>
    /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
    /// <exception cref="ArgumentNullException">filename or parent is null</exception>
    /// <exception cref="FileNotFoundException">filename is not found</exception>
    /// <exception cref="ArgumentException">parent is not valid (all types except <see cref="NodeType.File" /> are supported)</exception>
    public INode UploadFile(string filename, INode parent, CancellationToken? cancellationToken = null)
    {
      if (string.IsNullOrEmpty(filename))
      {
        throw new ArgumentNullException("filename");
      }

      if (parent == null)
      {
        throw new ArgumentNullException("parent");
      }

      if (!File.Exists(filename))
      {
        throw new FileNotFoundException(filename);
      }

      EnsureLoggedIn();

      var modificationDate = File.GetLastWriteTime(filename);
      using (var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read))
      {
        return Upload(fileStream, Path.GetFileName(filename), parent, modificationDate, cancellationToken);
      }
    }

    /// <summary>
    /// Upload a stream on Mega.co.nz and attach created node to selected parent
    /// </summary>
    /// <param name="stream">Data to upload</param>
    /// <param name="name">Created node name</param>
    /// <param name="modificationDate">Custom modification date stored in the Node attributes</param>
    /// <param name="parent">Node to attach the uploaded file (all types except <see cref="NodeType.File" /> are supported)</param>
    /// <param name="cancellationToken">CancellationToken used to cancel the action</param>
    /// <returns>Created node</returns>
    /// <exception cref="NotSupportedException">Not logged in</exception>
    /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
    /// <exception cref="ArgumentNullException">stream or name or parent is null</exception>
    /// <exception cref="ArgumentException">parent is not valid (all types except <see cref="NodeType.File" /> are supported)</exception>
    public INode Upload(Stream stream, string name, INode parent, DateTime? modificationDate = null, CancellationToken? cancellationToken = null)
    {
      if (stream == null)
      {
        throw new ArgumentNullException("stream");
      }

      if (string.IsNullOrEmpty(name))
      {
        throw new ArgumentNullException("name");
      }

      if (parent == null)
      {
        throw new ArgumentNullException("parent");
      }

      if (parent.Type == NodeType.File)
      {
        throw new ArgumentException("Invalid parent node");
      }

      if (parent is PublicNode)
      {
        throw new ApiException(ApiResultCode.AccessDenied);
      }

      EnsureLoggedIn();

      if (cancellationToken.HasValue)
      {
        stream = new CancellableStream(stream, cancellationToken.Value);
      }

      var completionHandle = string.Empty;
      var attempt = 0;
      while (_options.ComputeApiRequestRetryWaitDelay(++attempt, out var retryDelay))
      {
        // Retrieve upload URL
        var uploadRequest = new UploadUrlRequest(stream.Length);
        var uploadResponse = Request<UploadUrlResponse>(uploadRequest);

        var apiResult = ApiResultCode.Ok;
        using (var encryptedStream = new MegaAesCtrStreamCrypter(stream))
        {
          long chunkStartPosition = 0;
          var chunksSizesToUpload = ComputeChunksSizesToUpload(encryptedStream.ChunksPositions, encryptedStream.Length).ToArray();
          Uri uri = null;
          for (var i = 0; i < chunksSizesToUpload.Length; i++)
          {
            completionHandle = string.Empty;

            var chunkSize = chunksSizesToUpload[i];
            var chunkBuffer = new byte[chunkSize];
            encryptedStream.Read(chunkBuffer, 0, chunkSize);

            using (var chunkStream = new MemoryStream(chunkBuffer))
            {
              uri = new Uri(uploadResponse.Url + "/" + chunkStartPosition);
              chunkStartPosition += chunkSize;
              try
              {
                completionHandle = _webClient.PostRequestRaw(uri, chunkStream);
                if (string.IsNullOrEmpty(completionHandle))
                {
                  apiResult = ApiResultCode.Ok;
                  continue;
                }

                if (completionHandle.FromBase64().Length != 27 && long.TryParse(completionHandle, out var retCode))
                {
                  apiResult = (ApiResultCode)retCode;
                  break;
                }
              }
              catch (Exception ex)
              {
                apiResult = ApiResultCode.RequestFailedRetry;
                ApiRequestFailed?.Invoke(this, new ApiRequestFailedEventArgs(uri, attempt, retryDelay, apiResult, ex));

                break;
              }
            }
          }

          if (apiResult != ApiResultCode.Ok)
          {
            ApiRequestFailed?.Invoke(this, new ApiRequestFailedEventArgs(uri, attempt, retryDelay, apiResult, completionHandle));

            if (apiResult == ApiResultCode.RequestFailedRetry || apiResult == ApiResultCode.RequestFailedPermanetly || apiResult == ApiResultCode.TooManyRequests)
            {
              // Restart upload from the beginning
              Wait(retryDelay);

              // Reset steam position
              stream.Seek(0, SeekOrigin.Begin);

              continue;
            }

            throw new ApiException(apiResult);
          }

          // Encrypt attributes
          var cryptedAttributes = Crypto.EncryptAttributes(new Attributes(name, stream, modificationDate), encryptedStream.FileKey);

          // Compute the file key
          var fileKey = new byte[32];
          for (var i = 0; i < 8; i++)
          {
            fileKey[i] = (byte)(encryptedStream.FileKey[i] ^ encryptedStream.Iv[i]);
            fileKey[i + 16] = encryptedStream.Iv[i];
          }

          for (var i = 8; i < 16; i++)
          {
            fileKey[i] = (byte)(encryptedStream.FileKey[i] ^ encryptedStream.MetaMac[i - 8]);
            fileKey[i + 16] = encryptedStream.MetaMac[i - 8];
          }

          var encryptedKey = Crypto.EncryptKey(fileKey, _masterKey);

          var createNodeRequest = CreateNodeRequest.CreateFileNodeRequest(parent, cryptedAttributes.ToBase64(), encryptedKey.ToBase64(), fileKey, completionHandle);
          var createNodeResponse = Request<GetNodesResponse>(createNodeRequest, _masterKey);
          return createNodeResponse.Nodes[0];
        }
      }

      throw new UploadException(completionHandle);
    }

    /// <summary>
    /// Change node parent
    /// </summary>
    /// <param name="node">Node to move</param>
    /// <param name="destinationParentNode">New parent</param>
    /// <returns>Moved node</returns>
    /// <exception cref="NotSupportedException">Not logged in</exception>
    /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
    /// <exception cref="ArgumentNullException">node or destinationParentNode is null</exception>
    /// <exception cref="ArgumentException">node is not valid (only <see cref="NodeType.Directory" /> and <see cref="NodeType.File" /> are supported)</exception>
    /// <exception cref="ArgumentException">parent is not valid (all types except <see cref="NodeType.File" /> are supported)</exception>
    public INode Move(INode node, INode destinationParentNode)
    {
      if (node == null)
      {
        throw new ArgumentNullException("node");
      }

      if (destinationParentNode == null)
      {
        throw new ArgumentNullException("destinationParentNode");
      }

      if (node.Type != NodeType.Directory && node.Type != NodeType.File)
      {
        throw new ArgumentException("Invalid node type");
      }

      if (destinationParentNode.Type == NodeType.File)
      {
        throw new ArgumentException("Invalid destination parent node");
      }

      EnsureLoggedIn();

      Request(new MoveRequest(node, destinationParentNode));
      return GetNodes().First(n => n.Equals(node));
    }

    public INode Rename(INode node, string newName)
    {
      if (node == null)
      {
        throw new ArgumentNullException("node");
      }

      if (node.Type != NodeType.Directory && node.Type != NodeType.File)
      {
        throw new ArgumentException("Invalid node type");
      }

      if (string.IsNullOrEmpty(newName))
      {
        throw new ArgumentNullException("newName");
      }

      if (!(node is INodeCrypto nodeCrypto))
      {
        throw new ArgumentException("node must implement INodeCrypto");
      }

      EnsureLoggedIn();

      var encryptedAttributes = Crypto.EncryptAttributes(new Attributes(newName, ((Node)node).Attributes), nodeCrypto.Key);
      Request(new RenameRequest(node, encryptedAttributes.ToBase64()));
      return GetNodes().First(n => n.Equals(node));
    }

    /// <summary>
    /// Download thumbnail from file attributes (or return null if thumbnail is not available)
    /// </summary>
    /// <param name="node">Node to download the thumbnail from (only <see cref="NodeType.File" /> can be downloaded)</param>
    /// <param name="fileAttributeType">File attribute type to retrieve</param>
    /// <param name="cancellationToken">CancellationToken used to cancel the action</param>
    /// <exception cref="NotSupportedException">Not logged in</exception>
    /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
    /// <exception cref="ArgumentNullException">node or outputFile is null</exception>
    /// <exception cref="ArgumentException">node is not valid (only <see cref="NodeType.File" /> can be downloaded)</exception>
    /// <exception cref="InvalidOperationException">file attribute data is invalid</exception>
    public Stream DownloadFileAttribute(INode node, FileAttributeType fileAttributeType, CancellationToken? cancellationToken = null)
    {
      if (node == null)
      {
        throw new ArgumentNullException(nameof(node));
      }

      if (node.Type != NodeType.File)
      {
        throw new ArgumentException("Invalid node");
      }

      if (!(node is INodeCrypto nodeCrypto))
      {
        throw new ArgumentException("node must implement INodeCrypto");
      }

      EnsureLoggedIn();

      var fileAttribute = node.FileAttributes.FirstOrDefault(_ => _.Type == fileAttributeType);
      if (fileAttribute == null)
      {
        return null;
      }

      var downloadRequest = new DownloadFileAttributeRequest(fileAttribute.Handle);
      var downloadResponse = Request<DownloadFileAttributeResponse>(downloadRequest);

      var fileAttributeHandle = fileAttribute.Handle.FromBase64();
      using (var stream = _webClient.PostRequestRawAsStream(new Uri(downloadResponse.Url + "/0"), new MemoryStream(fileAttributeHandle)))
      {
        using (var memoryStream = new MemoryStream())
        {
          stream.CopyTo(memoryStream);
          memoryStream.Position = 0;

          const int DataOffset = 12; // handle (8) + position (4)
          var data = memoryStream.ToArray();
          var dataHandle = data.CopySubArray(8, 0);
          if (!dataHandle.SequenceEqual(fileAttributeHandle))
          {
            throw new InvalidOperationException($"File attribute handle mismatch ({fileAttribute.Handle} requested but {dataHandle.ToBase64()} received)");
          }

          var dataSize = BitConverter.ToUInt32(data.CopySubArray(4, 8), 0);
          if (dataSize != data.Length - DataOffset)
          {
            throw new InvalidOperationException($"File attribute size mismatch ({dataSize} expected but {data.Length - DataOffset} received)");
          }

          data = data.CopySubArray(data.Length - DataOffset, DataOffset);
          Stream resultStream = new MemoryStream(Crypto.DecryptAes(data, nodeCrypto.Key));
          if (cancellationToken.HasValue)
          {
            resultStream = new CancellableStream(resultStream, cancellationToken.Value);
          }

          return resultStream;
        }
      }
    }

    #endregion

    #region Private static methods

    private static string GenerateHash(string email, byte[] passwordAesKey)
    {
      var emailBytes = email.ToBytes();
      var hash = new byte[16];

      // Compute email in 16 bytes array
      for (var i = 0; i < emailBytes.Length; i++)
      {
        hash[i % 16] ^= emailBytes[i];
      }

      // Encrypt hash using password key
      using (var encryptor = Crypto.CreateAesEncryptor(passwordAesKey))
      {
        for (var it = 0; it < 16384; it++)
        {
          hash = Crypto.EncryptAes(hash, encryptor);
        }
      }

      // Retrieve bytes 0-4 and 8-12 from the hash
      var result = new byte[8];
      Array.Copy(hash, 0, result, 0, 4);
      Array.Copy(hash, 8, result, 4, 4);

      return result.ToBase64();
    }

    private static byte[] PrepareKey(byte[] data)
    {
      var pkey = new byte[] { 0x93, 0xC4, 0x67, 0xE3, 0x7D, 0xB0, 0xC7, 0xA4, 0xD1, 0xBE, 0x3F, 0x81, 0x01, 0x52, 0xCB, 0x56 };

      for (var it = 0; it < 65536; it++)
      {
        for (var idx = 0; idx < data.Length; idx += 16)
        {
          // Pad the data to 16 bytes blocks
          var key = data.CopySubArray(16, idx);

          pkey = Crypto.EncryptAes(pkey, key);
        }
      }

      return pkey;
    }

    #endregion

    #region Web

    private string Request(RequestBase request)
    {
      return Request<string>(request);
    }

    private TResponse Request<TResponse>(RequestBase request, byte[] key = null)
            where TResponse : class
    {
      if (_options.SynchronizeApiRequests)
      {
        lock (_apiRequestLocker)
        {
          return RequestCore<TResponse>(request, key);
        }
      }
      else
      {
        return RequestCore<TResponse>(request, key);
      }
    }

    private TResponse RequestCore<TResponse>(RequestBase request, byte[] key)
        where TResponse : class
    {
      var dataRequest = JsonConvert.SerializeObject(new object[] { request });
      var uri = GenerateUrl(request.QueryArguments);
      object jsonData = null;
      var attempt = 0;
      while (_options.ComputeApiRequestRetryWaitDelay(++attempt, out var retryDelay))
      {
        var dataResult = _webClient.PostRequestJson(uri, dataRequest);

        if (string.IsNullOrEmpty(dataResult)
          || (jsonData = JsonConvert.DeserializeObject(dataResult)) == null
          || jsonData is long
          || jsonData is JArray array && array[0].Type == JTokenType.Integer)
        {
          var apiCode = jsonData == null
            ? ApiResultCode.RequestFailedRetry
            : jsonData is long
              ? (ApiResultCode)Enum.ToObject(typeof(ApiResultCode), jsonData)
              : (ApiResultCode)((JArray)jsonData)[0].Value<int>();

          if (apiCode != ApiResultCode.Ok)
          {
            ApiRequestFailed?.Invoke(this, new ApiRequestFailedEventArgs(uri, attempt, retryDelay, apiCode, dataResult));
          }

          if (apiCode == ApiResultCode.RequestFailedRetry)
          {
            Wait(retryDelay);
            continue;
          }

          if (apiCode != ApiResultCode.Ok)
          {
            throw new ApiException(apiCode);
          }
        }

        break;
      }

      var data = ((JArray)jsonData)[0].ToString();
      return (typeof(TResponse) == typeof(string)) ? data as TResponse : JsonConvert.DeserializeObject<TResponse>(data, new GetNodesResponseConverter(key));
    }

    private void Wait(TimeSpan retryDelay)
    {
#if NET40
      Thread.Sleep(retryDelay);
#else
      Task
        .Delay(retryDelay)
        .Wait();
#endif
    }

    private Uri GenerateUrl(Dictionary<string, string> queryArguments)
    {
      var query = new Dictionary<string, string>(queryArguments)
      {
        ["id"] = (_sequenceIndex++ % uint.MaxValue).ToString(CultureInfo.InvariantCulture),
        ["ak"] = _options.ApplicationKey
      };

      if (!string.IsNullOrEmpty(_sessionId))
      {
        query["sid"] = _sessionId;
      }

#if NETSTANDARD1_3
      return new Uri(Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(s_baseApiUri.AbsoluteUri, query));
#else
      var builder = new UriBuilder(s_baseApiUri);
      var arguments = "";
      foreach (var item in query)
      {
        arguments = arguments + item.Key + "=" + item.Value + "&";
      }

      arguments = arguments.Substring(0, arguments.Length - 1);

      builder.Query = arguments;
      return builder.Uri;
#endif
    }

    private void SaveStream(Stream stream, string outputFile)
    {
      using (var fs = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write))
      {
        stream.CopyTo(fs, _options.BufferSize);
      }
    }

    #endregion

    #region Private methods

    private void EnsureLoggedIn()
    {
      if (_sessionId == null)
      {
        throw new NotSupportedException("Not logged in");
      }
    }

    private void EnsureLoggedOut()
    {
      if (_sessionId != null)
      {
        throw new NotSupportedException("Already logged in");
      }
    }

    private void GetPartsFromUri(Uri uri, out string id, out byte[] iv, out byte[] metaMac, out byte[] key)
    {
      if (!TryGetPartsFromUri(uri, out id, out var decryptedKey, out var isFolder)
          && !TryGetPartsFromLegacyUri(uri, out id, out decryptedKey, out isFolder))
      {
        throw new ArgumentException(string.Format("Invalid uri. Unable to extract Id and Key from the uri {0}", uri));
      }

      if (isFolder)
      {
        iv = null;
        metaMac = null;
        key = decryptedKey;
      }
      else
      {
        Crypto.GetPartsFromDecryptedKey(decryptedKey, out iv, out metaMac, out key);
      }
    }

    private bool TryGetPartsFromUri(Uri uri, out string id, out byte[] decryptedKey, out bool isFolder)
    {
      var uriRegex = new Regex(@"/(?<type>(file|folder))/(?<id>[^#]+)#(?<key>[^$/]+)");
      var match = uriRegex.Match(uri.PathAndQuery + uri.Fragment);
      if (match.Success)
      {
        id = match.Groups["id"].Value;
        decryptedKey = match.Groups["key"].Value.FromBase64();
        isFolder = match.Groups["type"].Value == "folder";
        return true;
      }
      else
      {
        id = null;
        decryptedKey = null;
        isFolder = default;
        return false;
      }
    }

    private bool TryGetPartsFromLegacyUri(Uri uri, out string id, out byte[] decryptedKey, out bool isFolder)
    {
      var uriRegex = new Regex(@"#(?<type>F?)!(?<id>[^!]+)!(?<key>[^$!\?]+)");
      var match = uriRegex.Match(uri.Fragment);
      if (match.Success)
      {
        id = match.Groups["id"].Value;
        decryptedKey = match.Groups["key"].Value.FromBase64();
        isFolder = match.Groups["type"].Value == "F";
        return true;
      }
      else
      {
        id = null;
        decryptedKey = null;
        isFolder = default;
        return false;
      }
    }

    private IEnumerable<int> ComputeChunksSizesToUpload(long[] chunksPositions, long streamLength)
    {
      for (var i = 0; i < chunksPositions.Length; i++)
      {
        var currentChunkPosition = chunksPositions[i];
        var nextChunkPosition = i == chunksPositions.Length - 1
          ? streamLength
          : chunksPositions[i + 1];

        // Pack multiple chunks in a single upload
        while (((int)(nextChunkPosition - currentChunkPosition) < _options.ChunksPackSize || _options.ChunksPackSize == -1) && i < chunksPositions.Length - 1)
        {
          i++;
          nextChunkPosition = i == chunksPositions.Length - 1
            ? streamLength
            : chunksPositions[i + 1];
        }

        yield return (int)(nextChunkPosition - currentChunkPosition);
      }
    }

    #endregion

    #region AuthInfos

    public class AuthInfos
    {
      public AuthInfos(string email, string hash, byte[] passwordAesKey, string mfaKey = null)
      {
        Email = email;
        Hash = hash;
        PasswordAesKey = passwordAesKey;
        MFAKey = mfaKey;
      }

      [JsonProperty]
      public string Email { get; private set; }

      [JsonProperty]
      public string Hash { get; private set; }

      [JsonProperty]
      public byte[] PasswordAesKey { get; private set; }

      [JsonProperty]
      public string MFAKey { get; private set; }
    }

    public class LogonSessionToken : IEquatable<LogonSessionToken>
    {
      [JsonProperty]
      public string SessionId { get; }

      [JsonProperty]
      public byte[] MasterKey { get; }

      private LogonSessionToken()
      {
      }

      public LogonSessionToken(string sessionId, byte[] masterKey)
      {
        SessionId = sessionId;
        MasterKey = masterKey;
      }

      public bool Equals(LogonSessionToken other)
      {
        if (other == null)
        {
          return false;
        }

        if (SessionId == null || other.SessionId == null || string.CompareOrdinal(SessionId, other.SessionId) != 0)
        {
          return false;
        }

        if (MasterKey == null || other.MasterKey == null || !Enumerable.SequenceEqual(MasterKey, other.MasterKey))
        {
          return false;
        }

        return true;
      }
    }

    #endregion

  }
}
