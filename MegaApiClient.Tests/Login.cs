using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CG.Web.MegaApiClient.Tests.Context;
using Moq;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace CG.Web.MegaApiClient.Tests
{
  [Collection(nameof(NotLoggedTestContext))]
  public class Login : TestsBase
  {
    public Login(NotLoggedTestContext context, ITestOutputHelper testOutputHelper)
      : base(context, testOutputHelper)
    {
    }

    public override void Dispose()
    {
      if (Context.Client.IsLoggedIn)
      {
        Context.Client.Logout();
      }

      base.Dispose();
    }

    [Fact]
    public void ClientCtor_NullOptions_Throws()
    {
      Assert.Throws<ArgumentNullException>("options", () => new MegaApiClient((Options)null));
    }

    [Fact]
    public void ClientCtor_NullWebClient_Throws()
    {
      Assert.Throws<ArgumentNullException>("webClient", () => new MegaApiClient((IWebClient)null));
    }

    [Fact]
    public void ClientCtor_NullOptionsAndNullWebClient_Throws()
    {
      Assert.Throws<ArgumentNullException>("options", () => new MegaApiClient(null, null));
    }

    [Theory, MemberData(nameof(InvalidCredentials))]
    public void Login_UnsupportedCredentials_Throws(string email, string password, string expectedMessage)
    {
      Assert.Throws<ArgumentNullException>(expectedMessage, () => Context.Client.Login(email, password));
    }

    public static IEnumerable<object[]> InvalidCredentials
    {
      get
      {
        yield return new object[] { null, null, "email" };
        yield return new object[] { null, "", "email" };
        yield return new object[] { "", null, "email" };
        yield return new object[] { "", "", "email" };
        yield return new object[] { null, "password", "email" };
        yield return new object[] { "username", null, "password" };
      }
    }

    [Theory]
    [InlineData("username", "password", ApiResultCode.BadArguments)]
    [InlineData("username@example.com", "password", ApiResultCode.RequestIncomplete)]
    public void Login_InvalidCredentials_Throws(string email, string password, ApiResultCode expectedErrorCode)
    {
      var exception = Assert.Throws<ApiException>(() => Context.Client.Login(email, password));
      Assert.Equal(expectedErrorCode, exception.ApiResultCode);
    }

    [Theory, MemberData(nameof(AllValidCredentials))]
    public void Login_ValidCredentials_Succeeds(string email, string password)
    {
      Assert.NotNull(Context.Client.Login(email, password));
      Assert.True(Context.Client.IsLoggedIn);
    }

    public static IEnumerable<object[]> GetCredentials(bool includeMasterKeyHash)
    {
      Assert.NotEmpty(AuthenticatedTestContext.Inputs.UsernameAccount);
      Assert.NotEmpty(AuthenticatedTestContext.Password);

      var credentials = new object[] { AuthenticatedTestContext.Inputs.UsernameAccount, AuthenticatedTestContext.Password, AuthenticatedTestContext.Inputs.MasterKeyHash };
      yield return includeMasterKeyHash ? credentials : credentials.Take(2).ToArray();
    }

    public static IEnumerable<object[]> AllValidCredentials => GetCredentials(false);

    public static IEnumerable<object[]> AllValidCredentialsWithHash => GetCredentials(true);

    [Theory, MemberData(nameof(AllValidCredentials))]
    public void LoginTwice_ValidCredentials_Throws(string email, string password)
    {
      Context.Client.Login(email, password);

      var exception = Assert.Throws<NotSupportedException>(() => Context.Client.Login(email, password));
      Assert.Equal("Already logged in", exception.Message);
    }

    [Fact]
    public void LoginAnonymous_Succeeds()
    {
      Context.Client.LoginAnonymous();
      Assert.True(Context.Client.IsLoggedIn);
    }

    [Fact]
    public void LoginAnonymousTwice_Throws()
    {
      Context.Client.LoginAnonymous();

      var exception = Assert.Throws<NotSupportedException>(() => Context.Client.LoginAnonymous());
      Assert.Equal("Already logged in", exception.Message);
    }

    [Theory, MemberData(nameof(AllValidCredentials))]
    public void LogoutAfterLogin_Succeeds(string email, string password)
    {
      Context.Client.Login(email, password);
      Assert.True(Context.Client.IsLoggedIn);

      Context.Client.Logout();
      Assert.False(Context.Client.IsLoggedIn);
    }

    [Theory, MemberData(nameof(AllValidCredentials))]
    public void LogoutTwiceAfterLogin_Throws(string email, string password)
    {
      Context.Client.Login(email, password);
      Context.Client.Logout();

      var exception = Assert.Throws<NotSupportedException>(() => Context.Client.Logout());
      Assert.Equal("Not logged in", exception.Message);
    }

    [Fact]
    public void LogoutWithoutLogin_Throws()
    {
      var exception = Assert.Throws<NotSupportedException>(() => Context.Client.Logout());
      Assert.Equal("Not logged in", exception.Message);
    }

    [Theory, MemberData(nameof(AllValidCredentialsWithHash))]
    public void GetRecoveryKeyAfterLogin_Succeeds(string email, string password, string expectedRecoveryKeyHash)
    {
      Context.Client.Login(email, password);
      Assert.True(Context.Client.IsLoggedIn);

      var recoveryKey = Context.Client.GetRecoveryKey();
      using var sha256 = SHA256.Create();
      var recoveryKeyHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(recoveryKey)).ToBase64();
      Assert.Equal(expectedRecoveryKeyHash, recoveryKeyHash);
    }

    [Fact]
    public void GetRecoveryKeyWithoutLogin_Throws()
    {
      var exception = Assert.Throws<NotSupportedException>(() => Context.Client.GetRecoveryKey());
      Assert.Equal("Not logged in", exception.Message);
    }

    [Fact]
    public void GetRecoveryKeyWithAnonymousLogin_Throws()
    {
      Context.Client.LoginAnonymous();
      var exception = Assert.Throws<NotSupportedException>(() => Context.Client.GetRecoveryKey());
      Assert.Equal("Anonymous login is not supported", exception.Message);
    }

    [Fact]
    public void Login_NullAuthInfos_Throws()
    {
      Assert.Throws<ArgumentNullException>("authInfos", () => Context.Client.Login((MegaApiClient.AuthInfos)null));
    }

    [Theory, MemberData(nameof(AllValidCredentials))]
    public void Login_DeserializedAuthInfos_Succeeds(string email, string password)
    {
      var authInfos = Context.Client.GenerateAuthInfos(email, password);
      var serializedAuthInfos = JsonConvert.SerializeObject(authInfos, Formatting.None).Replace('\"', '\'');
      var deserializedAuthInfos = JsonConvert.DeserializeObject<MegaApiClient.AuthInfos>(serializedAuthInfos);

      Context.Client.Login(deserializedAuthInfos);
      Assert.True(Context.Client.IsLoggedIn);
    }

    [Theory, MemberData(nameof(InvalidCredentials))]
    public void GenerateAuthInfos_InvalidCredentials_Throws(string email, string password, string expectedMessage)
    {
      Assert.Throws<ArgumentNullException>(expectedMessage, () => Context.Client.GenerateAuthInfos(email, password));
    }

    [Theory]
    [InlineData("username@example.com", "password", null, "{'Email':'username@example.com','Hash':'ObELy57HULI','PasswordAesKey':'ZAM5cl5uvROiXwBSEp98sQ==','MFAKey':null}")]
    [InlineData("username@example.com", "password", "mfa", "{'Email':'username@example.com','Hash':'ObELy57HULI','PasswordAesKey':'ZAM5cl5uvROiXwBSEp98sQ==','MFAKey':'mfa'}")]
    public void GenerateAuthInfos_ValidCredentials_Succeeds(string email, string password, string mfa, string expectedResult)
    {
      var authInfos = Context.Client.GenerateAuthInfos(email, password, mfa);
      var result = JsonConvert.SerializeObject(authInfos, Formatting.None).Replace('\"', '\'');

      Assert.Equal(expectedResult, result);
    }

    [Theory, MemberData(nameof(MethodsWithMandatoryLogin))]
    public void Methods_LoginRequired_Throws(Action<IMegaApiClient> testMethod)
    {
      var exception = Assert.Throws<NotSupportedException>(() => testMethod(Context.Client));
      Assert.Equal("Not logged in", exception.Message);
    }

    public static IEnumerable<object[]> MethodsWithMandatoryLogin()
    {
      var nodeDirectoryMock = new Mock<INode>();
      nodeDirectoryMock.SetupGet(x => x.Type).Returns(NodeType.Directory);
      nodeDirectoryMock.As<INodeCrypto>();
      var nodeDirectory = nodeDirectoryMock.Object;

      var nodeFileMock = new Mock<INode>();
      nodeFileMock.SetupGet(x => x.Type).Returns(NodeType.File);
      nodeFileMock.As<INodeCrypto>();
      var nodeFile = nodeFileMock.Object;

      var uri = new Uri("http://www.example.com");
      var tempFile = Path.GetTempFileName();

      yield return new object[] { (Action<IMegaApiClient>)(x => x.Delete(nodeDirectory)) };
      yield return new object[] { (Action<IMegaApiClient>)(x => x.Delete(nodeDirectory, false)) };
      yield return new object[] { (Action<IMegaApiClient>)(x => x.Delete(nodeDirectory, true)) };
      yield return new object[] { (Action<IMegaApiClient>)(x => x.DownloadFile(nodeFile, "outputFile")) };
      yield return new object[] { (Action<IMegaApiClient>)(x => x.DownloadFile(uri, "outputFile")) };
      yield return new object[] { (Action<IMegaApiClient>)(x => x.GetNodes()) };
      yield return new object[] { (Action<IMegaApiClient>)(x => x.GetNodes(nodeDirectory)) };
      yield return new object[] { (Action<IMegaApiClient>)(x => x.CreateFolder("name", nodeDirectory)) };
      yield return new object[] { (Action<IMegaApiClient>)(x => x.Download(nodeFile)) };
      yield return new object[] { (Action<IMegaApiClient>)(x => x.Download(uri)) };
      yield return new object[] { (Action<IMegaApiClient>)(x => x.GetDownloadLink(nodeDirectory)) };
      yield return new object[] { (Action<IMegaApiClient>)(x => x.Move(nodeDirectory, nodeDirectory)) };
      yield return new object[] { (Action<IMegaApiClient>)(x => x.Upload(new MemoryStream(new byte[0]), "name", nodeDirectory)) };
      yield return new object[] { (Action<IMegaApiClient>)(x => x.UploadFile(tempFile, nodeDirectory)) };
      yield return new object[] { (Action<IMegaApiClient>)(x => x.GetAccountInformation()) };
    }

    [Theory, MemberData(nameof(GetCredentials), false)]
    public void GetSessionHistory_AuthenticatedUser_Succeeds(string email, string password)
    {
      Context.Client.Login(email, password);

      var sessionsHistory = Context.Client.GetSessionsHistory();

      Assert.NotNull(sessionsHistory);
      var first = sessionsHistory.First();
      Assert.NotNull(first);
      Assert.Equal(SessionStatus.Current | SessionStatus.Active, first.Status);
      Assert.Equal(DateTime.UtcNow, first.LoginTime.ToUniversalTime(), TimeSpan.FromSeconds(30));
      Assert.Equal(DateTime.UtcNow, first.LastSeenTime.ToUniversalTime(), TimeSpan.FromSeconds(30));
    }
  }
}
