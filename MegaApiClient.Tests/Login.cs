

using System;
using NUnit.Framework;

namespace CG.Web.MegaApiClient.Tests
{
    [TestFixture]
    public class Login : TestsBase
    {
        public Login()
            : base(Options.None)
        {
        }

        [TestCase(null, null)]
        [TestCase(null, "")]
        [TestCase("", null)]
        [TestCase("", "")]
        [TestCase(null, "password")]
        [TestCase("username", null)]
        public void Login_UnsupportedCredentials_Throws(string username, string password)
        {
            Assert.That(
                () => this.Client.Login(username, password),
                Throws.TypeOf<ArgumentNullException>());
        }

        [TestCase("username", "password", ApiResultCode.BadArguments)]
        [TestCase("username@example.com", "password", ApiResultCode.ResourceNotExists)]
        public void Login_InvalidCredentials_Throws(string username, string password, ApiResultCode expectedErrorCode)
        {
            Assert.That(
                () => this.Client.Login(username, password),
                Throws.TypeOf<ApiException>()
                .With.Property("ApiResultCode").EqualTo(expectedErrorCode));
        }

        [TestCaseSource("GetCredentials")]
        public void Login_ValidCredentials_Succeeds(string username, string password)
        {
            Assert.That(
                () => this.Client.Login(username, password),
                Throws.Nothing);
        }

        [TestCaseSource("GetCredentials")]
        public void LoginTwice_ValidCredentials_Throws(string username, string password)
        {
            this.Client.Login(username, password);

            Assert.That(
                () => this.Client.Login(username, password),
                Throws.TypeOf<NotSupportedException>());
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
                Throws.TypeOf<NotSupportedException>());
        }

        [TestCaseSource("GetCredentials")]
        public void LogoutAfterLogin_Succeeds(string username, string password)
        {
            this.Client.Login(username, password);
            
            Assert.That(
                () => this.Client.Logout(),
                Throws.Nothing);
        }

        [Test]
        public void LogoutWithoutLogin_Throws()
        {
            Assert.That(
                () => this.Client.Logout(),
                Throws.TypeOf<NotSupportedException>());
        }
    }
}
