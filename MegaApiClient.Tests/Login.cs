

using System;
using System.Collections.Generic;
using System.IO;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace CG.Web.MegaApiClient.Tests
{
    [TestFixture]
    public class Login : TestsBase
    {
        public Login()
            : base(Options.None)
        {
        }

        [Test]
        public void ClientCtor_Succeeds()
        {
            Assert.That(
                () => this.Client = new MegaApiClient(),
                Throws.Nothing);
        }

        [Test]
        public void ClientCtor_NullOptions_Throws()
        {
            Assert.That(
                () => this.Client = new MegaApiClient((CG.Web.MegaApiClient.Options)null),
                Throws.TypeOf<ArgumentNullException>()
                .With.Property<ArgumentNullException>(x => x.ParamName).EqualTo("options"));
        }

        [Test]
        public void ClientCtor_NullWebClient_Throws()
        {
            Assert.That(
                () => this.Client = new MegaApiClient((IWebClient)null),
                Throws.TypeOf<ArgumentNullException>()
                .With.Property<ArgumentNullException>(x => x.ParamName).EqualTo("webClient"));
        }

        [Test]
        public void ClientCtor_NullOptionsAndNullWebClient_Throws()
        {
            Assert.That(
                () => this.Client = new MegaApiClient(null, null),
                Throws.TypeOf<ArgumentNullException>()
                .With.Property<ArgumentNullException>(x => x.ParamName).EqualTo("options"));
        }

        [TestCaseSource(typeof(Login), nameof(GetInvalidCredentials))]
        public void Login_UnsupportedCredentials_Throws(string email, string password)
        {
            Assert.That(
                () => this.Client.Login(email, password),
                Throws.TypeOf<ArgumentNullException>()
                .With.Property<ArgumentNullException>(x => x.ParamName).EqualTo("email")
                .Or.With.Property<ArgumentNullException>(x => x.ParamName).EqualTo("password"));
        }

        [TestCase("username", "password", ApiResultCode.BadArguments)]
        [TestCase("username@example.com", "password", ApiResultCode.ResourceNotExists)]
        public void Login_InvalidCredentials_Throws(string email, string password, ApiResultCode expectedErrorCode)
        {
            Assert.That(
                () => this.Client.Login(email, password),
                Throws.TypeOf<ApiException>()
                .With.Property<ApiException>(x => x.ApiResultCode).EqualTo(expectedErrorCode));
        }

        [TestCaseSource(typeof(TestsBase), nameof(GetCredentials))]
        public void Login_ValidCredentials_Succeeds(string email, string password)
        {
            Assert.That(
                () => this.Client.Login(email, password),
                Throws.Nothing);
        }

        [TestCaseSource(typeof(TestsBase), nameof(GetCredentials))]
        public void LoginTwice_ValidCredentials_Throws(string email, string password)
        {
            this.Client.Login(email, password);

            Assert.That(
                () => this.Client.Login(email, password),
                Throws.TypeOf<NotSupportedException>()
                .With.Message.EqualTo("Already logged in"));
        }

        [Test]
        public void LoginAnonymous_Succeeds()
        {
            Assert.That(
                () => this.Client.LoginAnonymous(),
                Throws.Nothing);
        }

        [Test]
        public void LoginAnonymousTwice_Throws()
        {
            this.Client.LoginAnonymous();

            Assert.That(
                () => this.Client.LoginAnonymous(),
                Throws.TypeOf<NotSupportedException>()
                .With.Message.EqualTo("Already logged in"));
        }

        [TestCaseSource(typeof(TestsBase), nameof(GetCredentials))]
        public void LogoutAfterLogin_Succeeds(string email, string password)
        {
            this.Client.Login(email, password);

            Assert.That(
                () => this.Client.Logout(),
                Throws.Nothing);
        }

        [TestCaseSource(typeof(TestsBase), nameof(GetCredentials))]
        public void LogoutTwiceAfterLogin_Throws(string email, string password)
        {
            this.Client.Login(email, password);

            this.Client.Logout();

            Assert.That(
                () => this.Client.Logout(),
                Throws.TypeOf<NotSupportedException>()
                .With.Message.EqualTo("Not logged in"));
        }

        [Test]
        public void LogoutWithoutLogin_Throws()
        {
            Assert.That(
                () => this.Client.Logout(),
                Throws.TypeOf<NotSupportedException>()
                .With.Message.EqualTo("Not logged in"));
        }

        [TestCase(null)]
        public void Login_NullAuthInfos_Throws(MegaApiClient.AuthInfos authInfos)
        {
            Assert.That(
                () => this.Client.Login(authInfos),
                Throws.TypeOf<ArgumentNullException>()
                .With.Property<ArgumentNullException>(x => x.ParamName).EqualTo("authInfos"));
        }

        [TestCaseSource(typeof(TestsBase), nameof(GetCredentials))]
        public void Login_DeserializedAuthInfos_Succeeds(string email, string password)
        {
            var authInfos = MegaApiClient.GenerateAuthInfos(email, password);
            var serializedAuthInfos = JsonConvert.SerializeObject(authInfos, Formatting.None).Replace('\"', '\'');
            var deserializedAuthInfos = JsonConvert.DeserializeObject<MegaApiClient.AuthInfos>(serializedAuthInfos);

            Assert.That(
                () => this.Client.Login(deserializedAuthInfos),
                Throws.Nothing);
        }

        [TestCaseSource(typeof(Login), nameof(GetInvalidCredentials))]
        public void GenerateAuthInfos_InvalidCredentials_Throws(string email, string password)
        {
            Assert.That(() =>
                MegaApiClient.GenerateAuthInfos(email, password),
                Throws.TypeOf<ArgumentNullException>()
                .With.Property<ArgumentNullException>(x => x.ParamName).EqualTo("email")
                .Or.With.Property<ArgumentNullException>(x => x.ParamName).EqualTo("password"));
        }

        [TestCase("username@example.com", "password", ExpectedResult = "{'Email':'username@example.com','Hash':'ObELy57HULI','PasswordAesKey':'ZAM5cl5uvROiXwBSEp98sQ=='}")]
        public string GenerateAuthInfos_ValidCredentials_Succeeds(string email, string password)
        {
            var authInfos = MegaApiClient.GenerateAuthInfos(email, password);

            return JsonConvert.SerializeObject(authInfos, Formatting.None).Replace('\"', '\'');
        }

        [TestCaseSource(typeof(Login), nameof(GetMethodsRequiredLogin))]
        public void Methods_LoginRequired_Throws(Action<IMegaApiClient> testMethod)
        {
            Assert.That(
                () => testMethod(this.Client),
                Throws.TypeOf<NotSupportedException>()
                .With.Message.EqualTo("Not logged in"));
        }

        [TestCaseSource(typeof(TestsBase), nameof(GetCredentials))]
        public void GetAccountInformation_AuthenticatedUser_Succeeds(string email, string password)
        {
            this.Client.Login(email, password);

            IAccountInformation accountInformation = this.Client.GetAccountInformation();

            Assert.That(accountInformation, Is.Not.Null
                .And.Property<IAccountInformation>(x => x.TotalQuota).EqualTo(53687091200)
                .And.Property<IAccountInformation>(x => x.UsedQuota).EqualTo(523265));
        }

        [Test]
        public void GetAccountInformation_AnonymousUser_Succeeds()
        {
            this.Client.LoginAnonymous();

            IAccountInformation accountInformation = this.Client.GetAccountInformation();

            Assert.That(accountInformation, Is.Not.Null
                .And.Property<IAccountInformation>(x => x.TotalQuota).EqualTo(53687091200)
                .And.Property<IAccountInformation>(x => x.UsedQuota).EqualTo(0));
        }

        [Test]
        public void GetAccountInformation_NotLoggedIn_Throws()
        {
            Assert.That(
                () => this.Client.GetAccountInformation(),
                Throws.TypeOf<NotSupportedException>()
                .With.Message.EqualTo("Not logged in"));
        }

        private static IEnumerable<ITestCaseData> GetInvalidCredentials()
        {
            yield return new TestCaseData(null, null);
            yield return new TestCaseData(null, "");
            yield return new TestCaseData("", null);
            yield return new TestCaseData("", "");
            yield return new TestCaseData(null, "password");
            yield return new TestCaseData("username", null);
        }

        private static IEnumerable<ITestCaseData> GetMethodsRequiredLogin()
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

            yield return new TestCaseData((Action<IMegaApiClient>)(x => x.Delete(nodeDirectory)));
            yield return new TestCaseData((Action<IMegaApiClient>)(x => x.Delete(nodeDirectory, false)));
            yield return new TestCaseData((Action<IMegaApiClient>)(x => x.Delete(nodeDirectory, true)));
            yield return new TestCaseData((Action<IMegaApiClient>)(x => x.DownloadFile(nodeFile, "outputFile")));
            yield return new TestCaseData((Action<IMegaApiClient>)(x => x.DownloadFile(uri, "outputFile")));
            yield return new TestCaseData((Action<IMegaApiClient>)(x => x.GetNodes()));
            yield return new TestCaseData((Action<IMegaApiClient>)(x => x.GetNodes(nodeDirectory)));
            yield return new TestCaseData((Action<IMegaApiClient>)(x => x.CreateFolder("name", nodeDirectory)));
            yield return new TestCaseData((Action<IMegaApiClient>)(x => x.Download(nodeFile)));
            yield return new TestCaseData((Action<IMegaApiClient>)(x => x.Download(uri)));
            yield return new TestCaseData((Action<IMegaApiClient>)(x => x.GetDownloadLink(nodeDirectory)));
            yield return new TestCaseData((Action<IMegaApiClient>)(x => x.Move(nodeDirectory, nodeDirectory)));
            yield return new TestCaseData((Action<IMegaApiClient>)(x => x.Upload(new MemoryStream(new byte[0]), "name", nodeDirectory)));
            yield return new TestCaseData((Action<IMegaApiClient>)(x => x.UploadFile(tempFile, nodeDirectory)));
        }
    }
}
