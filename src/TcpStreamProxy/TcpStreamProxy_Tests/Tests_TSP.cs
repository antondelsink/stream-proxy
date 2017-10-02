using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TcpStreamProxy.Tests
{
    [TestClass]
    public class Tests_TSP
    {
        private IPEndPoint listenOn = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 55000);
        private IPEndPoint forwardTo = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6379);

        /// <summary>
        /// Instantiate the Proxy and call .Start to cause it to listen on the specified port.
        /// </summary>
        [TestMethod]
        public void Test_01_Start_Wait_Dispose()
        {
            using (var p = new Proxy(listenOn, forwardTo))
            {
                p.Start();

                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Instantiate the Proxy and call .Start to cause it to listen on the specified port.
        /// </summary>
        [TestMethod]
        public void Test_02_Start_Wait90s_Dispose()
        {
            using (var p = new Proxy(listenOn, forwardTo))
            {
                p.Start();

                Thread.Sleep(90 * 1000);
            }
        }

        /// <summary>
        /// Instantiate the Proxy and call .Start to cause it to listen on the specified port.
        /// Launches the Redis Benchmark command-line tool to send a little traffic through the proxy to a local Redis instance.
        /// </summary>
        /// <remarks>
        /// Note: this method assumes the global variables listenOn and forwardTo are set as follows:
        /// listenOn must be the local address 127.0.0.1 and port 55000.
        /// forwardTo must be the local address 127.0.0.1 and port 6379.
        /// </remarks>
        [TestMethod]
        public void Test_10_RedisBenchmark_1Client_1kOps() // should run for less than 10s
        {
            using (var p = new Proxy(listenOn, forwardTo))
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
        }

        /// <summary>
        /// Instantiate the Proxy and call .Start to cause it to listen on the specified port.
        /// Launches the Redis Benchmark command-line tool to send a lot of traffic through the proxy to a local Redis instance.
        /// </summary>
        /// <remarks>
        /// Note: this method assumes the global variables listenOn and forwardTo are set as follows:
        /// listenOn must be the local address 127.0.0.1 and port 55000.
        /// forwardTo must be the local address 127.0.0.1 and port 6379.
        /// </remarks>
        [TestMethod]
        public void Test_11_RedisBenchmark_50Clients_100kOps()
        {
            using (var p = new Proxy(listenOn, forwardTo))
            {
                p.Start();

                var proc = 
                    Process.Start(
                        new ProcessStartInfo(
                            "redis-benchmark.exe",
                            "-c 50 -n 100000 -d 128 -t SET,GET -p 55000")
                {
                    RedirectStandardOutput = true
                });
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                File.WriteAllText("redis-benchmark_50c100k.output", output);
            }
        }

        /// <summary>
        /// An in-memory log of traffic between the redis-benchmark and redis server (both directions).
        /// </summary>
        private static StringBuilder _log = new StringBuilder();

        /// <summary>
        /// Instantiate the Proxy and call .Start to cause it to listen on the specified port.
        /// Launches the Redis Benchmark command-line tool to send traffic through the proxy to a local Redis instance.
        /// </summary>
        /// <remarks>
        /// Note: this method assumes the global variables listenOn and forwardTo are set as follows:
        /// listenOn must be the local address 127.0.0.1 and port 55000.
        /// forwardTo must be the local address 127.0.0.1 and port 6379.
        /// </remarks>
        [TestMethod]
        public void Test_20_RedisBenchmark_Logged() // should run for less than 10s
        {
            Action<byte[], int> logger = (bytes, len) =>
            {
                lock (_log)
                {
                    _log.AppendLine(
                            Encoding.UTF8.GetString(bytes, 0, len)
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