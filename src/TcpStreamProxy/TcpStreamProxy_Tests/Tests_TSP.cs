using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace TcpStreamProxy.Tests
{
    [TestClass]
    public class Tests_TSP
    {
        private IPEndPoint listenOn = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 55000);
        private IPEndPoint forwardTo = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6379);

        [TestMethod]
        public void Test_01_Start_Wait_Dispose()
        {
            using (var p = new Proxy(listenOn, forwardTo))
            {
                p.Start();

                Thread.Sleep(100);
            }
        }

        [TestMethod]
        public void Test_02_Start_Wait90seconds()
        {
            using (var p = new Proxy(listenOn, forwardTo))
            {
                p.Start();

                Thread.Sleep(90 * 1000);
            }
        }

        [TestMethod]
        public void Test_10_RedisBenchmark_1Client_1kOps() // should run for less than 10s
        {
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

        private static StringBuilder _log = new StringBuilder();

        [TestMethod]
        public void Test_10_RedisBenchmark_Logged() // should run for less than 10s
        {
            Action<byte[]> logger = (bytes) =>
            {
                lock (_log)
                {
                    _log.AppendLine(
                            Encoding.UTF8.GetString(bytes)
                                .Replace("\r\n", "\\r\\n"));
                }
            };

            using (var p = new Proxy(listenOn, forwardTo, logger))
            {
                p.Start();

                var proc =
                    Process.Start(
                        new ProcessStartInfo(
                            "redis-benchmark.exe",
                            "-c 1 -n 1000 -d 128 -t SET,GET -p 55000")
                        {
                            RedirectStandardOutput = true
                        });

                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                File.WriteAllText("redis-benchmark_1c1k.output", output);
            }

            File.WriteAllText("redis-benchmark_1c1k.log", _log.ToString());
        }
    }
}