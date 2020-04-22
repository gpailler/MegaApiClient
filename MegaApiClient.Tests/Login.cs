﻿using System;
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
      if (this.context.Client.IsLoggedIn)
      {
        this.context.Client.Logout();
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
      Assert.Throws<ArgumentNullException>(expectedMessage, () => this.context.Client.Login(email, password));
    }

    public static IEnumerable<object[]> InvalidCredentials
    {
      get
      {
        yield return new object[] { null, null, "email"};
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
      var exception = Assert.Throws<ApiException>(() => this.context.Client.Login(email, password));
      Assert.Equal(expectedErrorCode, exception.ApiResultCode);
    }

    [Theory, MemberData(nameof(AllValidCredentials))]
    public void Login_ValidCredentials_Succeeds(string email, string password)
    {
      Assert.NotNull(this.context.Client.Login(email, password));
      Assert.True(this.context.Client.IsLoggedIn);
    }

    public static IEnumerable<object[]> GetCredentialsV1(bool includeMasterKeyHash)
    {
      Assert.NotEmpty(AuthenticatedTestContext.UsernameAccountV1);
      Assert.NotEmpty(AuthenticatedTestContext.Password);

      var credentials = new object[] {AuthenticatedTestContext.UsernameAccountV1, AuthenticatedTestContext.Password, AuthenticatedTestContext.MasterKeyHashV1 };
      yield return includeMasterKeyHash ? credentials : credentials.Take(2).ToArray();
    }

    public static IEnumerable<object[]> GetCredentialsV2(bool includeMasterKeyHash)
    {
      Assert.NotEmpty(AuthenticatedTestContext.UsernameAccountV2);
      Assert.NotEmpty(AuthenticatedTestContext.Password);

      var credentials = new object[] { AuthenticatedTestContext.UsernameAccountV2, AuthenticatedTestContext.Password, AuthenticatedTestContext.MasterKeyHashV2 };
      yield return includeMasterKeyHash ? credentials : credentials.Take(2).ToArray();
    }

    public static IEnumerable<object[]> AllValidCredentials => GetCredentialsV1(false).Concat(GetCredentialsV2(false));

    public static IEnumerable<object[]> AllValidCredentialsWithHash => GetCredentialsV1(true).Concat(GetCredentialsV2(true));

    [Theory, MemberData(nameof(AllValidCredentials))]
    public void LoginTwice_ValidCredentials_Throws(string email, string password)
    {
      this.context.Client.Login(email, password);

      var exception = Assert.Throws<NotSupportedException>(() => this.context.Client.Login(email, password));
      Assert.Equal("Already logged in", exception.Message);
    }

    [Fact]
    public void LoginAnonymous_Succeeds()
    {
      this.context.Client.LoginAnonymous();
      Assert.True(this.context.Client.IsLoggedIn);
    }

    [Fact]
    public void LoginAnonymousTwice_Throws()
    {
      this.context.Client.LoginAnonymous();

      var exception = Assert.Throws<NotSupportedException>(() => this.context.Client.LoginAnonymous());
      Assert.Equal("Already logged in", exception.Message);
    }

    [Theory, MemberData(nameof(AllValidCredentials))]
    public void LogoutAfterLogin_Succeeds(string email, string password)
    {
      this.context.Client.Login(email, password);
      Assert.True(this.context.Client.IsLoggedIn);

      this.context.Client.Logout();
      Assert.False(this.context.Client.IsLoggedIn);
    }

    [Theory, MemberData(nameof(AllValidCredentials))]
    public void LogoutTwiceAfterLogin_Throws(string email, string password)
    {
      this.context.Client.Login(email, password);
      this.context.Client.Logout();

      var exception = Assert.Throws<NotSupportedException>(() => this.context.Client.Logout());
      Assert.Equal("Not logged in", exception.Message);
    }

    [Fact]
    public void LogoutWithoutLogin_Throws()
    {
      var exception = Assert.Throws<NotSupportedException>(() => this.context.Client.Logout());
      Assert.Equal("Not logged in", exception.Message);
    }

    [Theory, MemberData(nameof(AllValidCredentialsWithHash))]
    public void GetRecoveryKeyAfterLogin_Succeeds(string email, string password, string expectedRecoveryKeyHash)
    {
      this.context.Client.Login(email, password);
      Assert.True(this.context.Client.IsLoggedIn);

      var recoveryKey = this.context.Client.GetRecoveryKey();
      using (var sha256 = SHA256.Create())
      {
        var recoveryKeyHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(recoveryKey)).ToBase64();
        Assert.Equal(expectedRecoveryKeyHash, recoveryKeyHash);
      }
    }

    [Fact]
    public void GetRecoveryKeyWithoutLogin_Throws()
    {
      var exception = Assert.Throws<NotSupportedException>(() => this.context.Client.GetRecoveryKey());
      Assert.Equal("Not logged in", exception.Message);
    }

    [Fact]
    public void GetRecoveryKeyWithAnonymousLogin_Throws()
    {
      this.context.Client.LoginAnonymous();
      var exception = Assert.Throws<NotSupportedException>(() => this.context.Client.GetRecoveryKey());
      Assert.Equal("Anonymous login is not supported", exception.Message);
    }

    [Fact]
    public void Login_NullAuthInfos_Throws()
    {
      Assert.Throws<ArgumentNullException>("authInfos", () => this.context.Client.Login((MegaApiClient.AuthInfos)null));
    }

    [Theory, MemberData(nameof(AllValidCredentials))]
    public void Login_DeserializedAuthInfos_Succeeds(string email, string password)
    {
      var authInfos = this.context.Client.GenerateAuthInfos(email, password);
      var serializedAuthInfos = JsonConvert.SerializeObject(authInfos, Formatting.None).Replace('\"', '\'');
      var deserializedAuthInfos = JsonConvert.DeserializeObject<MegaApiClient.AuthInfos>(serializedAuthInfos);

      this.context.Client.Login(deserializedAuthInfos);
      Assert.True(this.context.Client.IsLoggedIn);
    }

    [Theory, MemberData(nameof(InvalidCredentials))]
    public void GenerateAuthInfos_InvalidCredentials_Throws(string email, string password, string expectedMessage)
    {
      Assert.Throws<ArgumentNullException>(expectedMessage, () => this.context.Client.GenerateAuthInfos(email, password));
    }

    [Theory, InlineData("username@example.com", "password", "{'Email':'username@example.com','Hash':'ObELy57HULI','PasswordAesKey':'ZAM5cl5uvROiXwBSEp98sQ=='}")]
    public void GenerateAuthInfos_ValidCredentials_Succeeds(string email, string password, string expectedResult)
    {
      var authInfos = this.context.Client.GenerateAuthInfos(email, password);
      var result = JsonConvert.SerializeObject(authInfos, Formatting.None).Replace('\"', '\'');

      Assert.Equal(expectedResult, result);
    }

    [Theory, MemberData(nameof(MethodsWithMandatoryLogin))]
    public void Methods_LoginRequired_Throws(Action<IMegaApiClient> testMethod)
    {
      var exception = Assert.Throws<NotSupportedException>(() => testMethod(this.context.Client));
      Assert.Equal("Not logged in", exception.Message);
    }

    public static IEnumerable<object[]> MethodsWithMandatoryLogin()
    {
      Mock<INode> nodeDirectoryMock = new Mock<INode>();
      nodeDirectoryMock.SetupGet(x => x.Type).Returns(NodeType.Directory);
      nodeDirectoryMock.As<INodeCrypto>();
      INode nodeDirectory = nodeDirectoryMock.Object;

      Mock<INode> nodeFileMock = new Mock<INode>();
      nodeFileMock.SetupGet(x => x.Type).Returns(NodeType.File);
      nodeFileMock.As<INodeCrypto>();
      INode nodeFile = nodeFileMock.Object;

      Uri uri = new Uri("http://www.example.com");
      string tempFile = Path.GetTempFileName();

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

    [Theory, MemberData(nameof(GetCredentialsV1), false)]
    public void GetAccountInformation_AuthenticatedUserV1_Succeeds(string email, string password)
    {
      this.context.Client.Login(email, password);

      var authenticatedTestContext = new AuthenticatedTestContext();
      var protectedNodes = authenticatedTestContext.ProtectedNodes;
      this.SanitizeStorage(protectedNodes);

      IAccountInformation accountInformation = this.context.Client.GetAccountInformation();

      Assert.NotNull(accountInformation);
      Assert.Equal(53687091200, accountInformation.TotalQuota);
      Assert.Equal(AuthenticatedTestContext.FileSize + AuthenticatedTestContext.SubFolderFileSize, accountInformation.UsedQuota);
    }

    [Theory, MemberData(nameof(GetCredentialsV2), false)]
    public void GetAccountInformation_AuthenticatedUserV2_Succeeds(string email, string password)
    {
      this.context.Client.Login(email, password);

      IAccountInformation accountInformation = this.context.Client.GetAccountInformation();

      Assert.NotNull(accountInformation);
      Assert.Equal(16106127360, accountInformation.TotalQuota);
      Assert.Equal(0, accountInformation.UsedQuota);
    }

    [Theory, MemberData(nameof(GetCredentialsV2), false)]
    public void GetSessionHistory_AuthenticatedUserV2_Succeeds(string email, string password)
    {
      this.context.Client.Login(email, password);

      IEnumerable<ISession> sessionsHistory = this.context.Client.GetSessionsHistory();

      Assert.NotNull(sessionsHistory);
      ISession first = sessionsHistory.First();
      Assert.NotNull(first);
      Assert.Equal(SessionStatus.Current | SessionStatus.Active, first.Status);
      Assert.Equal(DateTime.UtcNow, first.LoginTime, TimeSpan.FromSeconds(30));
      Assert.Equal(DateTime.UtcNow, first.LastSeenTime, TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void GetAccountInformation_AnonymousUser_Succeeds()
    {
      this.context.Client.LoginAnonymous();

      IAccountInformation accountInformation = this.context.Client.GetAccountInformation();

      Assert.NotNull(accountInformation);
      Assert.Equal(16106127360, accountInformation.TotalQuota);
      Assert.Equal(0, accountInformation.UsedQuota);
    }
  }
}
