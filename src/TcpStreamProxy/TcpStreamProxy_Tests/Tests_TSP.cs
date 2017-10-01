using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

namespace TcpStreamProxy_Tests
{
    [TestClass]
    public class Tests_TSP
    {

        [TestMethod]
        public void Test_01_Start_and_Dispose()
        {
            var listenOn = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 55000);
            var forwardTo = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6379);

            using (var p = new TcpStreamProxy.Proxy(listenOn, forwardTo))
            {
                p.Start();
            }
        }
    }
}
