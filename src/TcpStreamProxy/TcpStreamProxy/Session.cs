using System.Net.Sockets;

namespace TcpStreamProxy
{
    internal class Session
    {
        public const int BufferSize = 4 * 1024;

        public string ClientSocketInfo;

        public NetworkStream Client;
        public NetworkStream Server;

        public byte[] ClientSendBuffer = new byte[BufferSize];
        public byte[] ServerSendBuffer = new byte[BufferSize];

        public bool ClientAsyncIoInProgress = false;
        public void SetClientAsyncIoInProgressFalse()
        {
            ClientAsyncIoInProgress = false;
        }

        public bool ServerAsyncIoInProgress = false;
        public void SetServerAsyncIoInProgressFalse()
        {
            ServerAsyncIoInProgress= false;
        }
    }
}
