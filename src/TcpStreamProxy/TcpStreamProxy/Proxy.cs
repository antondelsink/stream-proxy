using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TcpStreamProxy
{
    public class Proxy : IDisposable
    {
        private TcpListener _tcpListener = null;

        private IPEndPoint _forwardTo = null;

        private ConcurrentBag<Session> _sessions = new ConcurrentBag<Session>();

        private TcpStreamCopier _streamCopier = null;

        private Action<byte[], int> _logger = null;

        public Proxy(IPEndPoint listenOn, IPEndPoint forwardTo, Action<byte[], int> logger = null)
        {
            _logger = logger;

            _forwardTo = forwardTo;

            _tcpListener = new TcpListener(listenOn);

            _streamCopier = new TcpStreamCopier(_sessions, _logger);
        }

        public void Dispose()
        {
            if (_tcpListener != null)
            {
                _tcpListener.Stop();
                _tcpListener = null;
            }

            if (_sessions != null)
            {
                _sessions.Clear();
                _sessions = null;
            }

            if (_streamCopier != null)
            {
                _streamCopier.Dispose();
                _streamCopier = null;
            }

            GC.SuppressFinalize(this);
        }

        public void Start()
        {
            _tcpListener.Start();
            _tcpListener.AcceptSocketAsync().ContinueWith(OnTcpSocketAccept);

            _streamCopier.Start();
        }

        private void OnTcpSocketAccept(Task<Socket> task)
        {
            CreateSession(task.Result);

            AcceptSocketAsync();
        }

        private void AcceptSocketAsync()
        {
            if (_tcpListener != null)
            {
                _tcpListener.AcceptSocketAsync().ContinueWith(OnTcpSocketAccept);
            }
        }

        private void CreateSession(Socket socket)
        {
            try
            {
                _sessions.Add(
                    new Session()
                    {
                        ClientSocketInfo = ToString(socket),
                        Client = new NetworkStream(socket, true),
                        Server = GetNewServerStream()
                    });

            }
            catch { }
        }

        private static string ToString(Socket socket)
        {
            var ep = (IPEndPoint)socket.RemoteEndPoint;
            return $"{ep.Address.ToString()}_{ep.Port.ToString()}";
        }

        public NetworkStream GetNewServerStream()
        {
            var target = new TcpClient();
            target.Connect(_forwardTo);
            return target.GetStream();
        }
    }
}