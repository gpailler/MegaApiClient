

using System;
using NUnit.Framework;

namespace CG.Web.MegaApiClient.Tests
{
    public class LoginTests
    {
        [TestCase(null, null)]
        [TestCase(null, "")]
        [TestCase("", null)]
        [TestCase("", "")]
        [TestCase(null, "password")]
        [TestCase("username", null)]
        public void Login_UnsupportedCredentials_Throws(string username, string password)
        {
            // Arrange
            var client = new MegaApiClient();

            // Act + Assert
            Assert.Throws<ArgumentNullException>(() => client.Login(username, password));
        }

        [TestCase("username", "password", ApiResultCode.BadArguments)]
        [TestCase("username@example.com", "password", ApiResultCode.ResourceNotExists)]
        public void Login_InvalidCredentials_Throws(string username, string password, ApiResultCode expectedErrorCode)
        {
            // Arrange
            var client = new MegaApiClient();

            // Act + Assert
            ApiException e = Assert.Throws<ApiException>(() => client.Login(username, password));
            Assert.AreEqual(expectedErrorCode, e.ApiResultCode);
        }

        [TestCase("megaapiclient@yopmail.com", "megaapiclient")]
        public void Login_ValidCredentials_Succeeds(string username, string password)
        {
            // Arrange
            var client = new MegaApiClient();

            // Act + Assert
            Assert.DoesNotThrow(() => client.Login(username, password));
        }

        [TestCase("megaapiclient@yopmail.com", "megaapiclient")]
        public void LoginTwice_ValidCredentials_Throws(string username, string password)
        {
            // Arrange
            var client = new MegaApiClient();

            // Act
            client.Login(username, password);
                
            // Assert
            Assert.Throws<NotSupportedException>(() => client.Login(username, password));
        }

        [Test]
        public void LoginAnonymous_Succeeds()
        {
            // Arrange
            var client = new MegaApiClient();

            // Act + Assert
            Assert.DoesNotThrow(() => client.LoginAnonymous());
        }

        [Test]
        public void LoginAnonymousTwice_Throws()
        {
            // Arrange
            var client = new MegaApiClient();

            // Act
            client.LoginAnonymous();

            // Assert
            Assert.Throws<NotSupportedException>(() => client.LoginAnonymous());
        }

        [TestCase("megaapiclient@yopmail.com", "megaapiclient")]
        public void LogoutAfterLogin_Succeeds(string username, string password)
        {
            // Arrange
            var client = new MegaApiClient();

            // Act
            client.Login(username, password);

            // Assert
            Assert.DoesNotThrow(() => client.Logout());
        }

        [Test]
        public void LogoutWithoutLogin_Throws()
        {
            // Arrange
            var client = new MegaApiClient();

            // Act + Assert
            Assert.Throws<NotSupportedException>(() => client.Logout());
        }
    }
}
