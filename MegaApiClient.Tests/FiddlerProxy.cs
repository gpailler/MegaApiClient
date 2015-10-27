using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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

            FiddlerApplication.AfterSessionComplete += FiddlerApplication_AfterSessionComplete;
            FiddlerApplication.Startup(1234, FiddlerCoreStartupFlags.Default);
        }

        [TearDown]
        public void Teardown()
        {
            FiddlerApplication.AfterSessionComplete -= FiddlerApplication_AfterSessionComplete;
            FiddlerApplication.Shutdown();
        }

        private void FiddlerApplication_AfterSessionComplete(Session oSession)
        {
            Console.WriteLine(oSession.fullUrl);
            Console.WriteLine(oSession.responseCode);
        }
    }
}
