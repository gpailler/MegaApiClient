using System;
using System.Collections.Generic;
using System.Linq;
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
