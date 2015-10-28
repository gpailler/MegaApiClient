using System.Net;
using Fiddler;
using NUnit.Framework;

namespace CG.Web.MegaApiClient.Tests
{
    [SetUpFixture]
    public class FiddlerProxy
    {
        [SetUp]
        public void Setup()
        {
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
            FiddlerApplication.Startup(1234, FiddlerCoreStartupFlags.Default);
        }

        [TearDown]
        public void Teardown()
        {
            FiddlerApplication.Shutdown();
        }
    }
}
