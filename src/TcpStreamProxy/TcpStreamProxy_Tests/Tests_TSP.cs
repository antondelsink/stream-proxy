using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;

namespace TcpStreamProxy.Tests
{
    [TestClass]
    public class Tests_TSP
    {
        [TestMethod]
        public void Test_01_Start_Wait_Dispose()
        {
            var listenOn = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 55000);
            var forwardTo = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6379);

            using (var p = new Proxy(listenOn, forwardTo))
            {
                p.Start();

                Thread.Sleep(100);
            }
        }

        [TestMethod]
        public void Test_02_Start_Wait90seconds()
        {
            ThreadPool.SetMinThreads(50, 50);
            ThreadPool.SetMaxThreads(150, 150);

            var listenOn = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 55000);
            var forwardTo = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6379);

            using (var p = new Proxy(listenOn, forwardTo))
            {
                p.Start();

                Thread.Sleep(90 * 1000);
            }
        }

        [TestMethod]
        public void Test_10_RedisBenchmark_1Client_1kOps() // should run for less than 10s
        {
            var listenOn = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 55000);
            var forwardTo = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6379);

            using (var p = new Proxy(listenOn, forwardTo))
            {
                p.Start();

                var proc = Process.Start(new ProcessStartInfo("redis-benchmark.exe", "-c 1 -n 1000 -d 128 -t SET,GET -p 55000")
                {
                    RedirectStandardOutput = true
                });
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                File.WriteAllText("redis-benchmark_1c1k.output", output);
            }
        }

        [TestMethod]
        public void Test_11_RedisBenchmark_50Clients_100kOps()
        {
            var listenOn = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 55000);
            var forwardTo = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6379);

            using (var p = new Proxy(listenOn, forwardTo))
            {
                p.Start();

                var proc = Process.Start(new ProcessStartInfo("redis-benchmark.exe", "-c 50 -n 100000 -d 128 -t SET,GET -p 55000")
                {
                    RedirectStandardOutput = true
                });
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                File.WriteAllText("redis-benchmark_50c100k.output", output);
            }
        }
    }
}