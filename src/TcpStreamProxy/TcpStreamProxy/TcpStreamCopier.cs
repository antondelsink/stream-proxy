﻿using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TcpStreamProxy
{
    internal class TcpStreamCopier : IDisposable
    {
        private Thread _backgroundThread = null;

        private ConcurrentBag<Session> _sessions = null;

        private Action<byte[], int> _logger = null;

        public TcpStreamCopier(ConcurrentBag<Session> sessions, Action<byte[], int> logger = null)
        {
            _logger = logger;
            _sessions = sessions;

            _backgroundThread = new Thread(new ThreadStart(ProcessClientNetworkStreams))
            {
                Name = $"TCP Stream Proxy: Stream Copier Thread",
                IsBackground = true
            };
        }

        public void Start()
        {
            if (_backgroundThread == null) throw new InvalidOperationException();

            _backgroundThread.Start();
        }

        public void Dispose()
        {
            _logger = null;
            _backgroundThread = null;

            if (_sessions != null)
            {
                _sessions.Clear();
                _sessions = null;
            }

            GC.SuppressFinalize(this);
        }

        private void ProcessClientNetworkStreams()
        {
            while (_sessions != null)
            {
                var localSessions = _sessions;
                if (localSessions != null)
                {
                    foreach (var s in localSessions)
                    {
                        try
                        {
                            if (!s.ClientAsyncIoInProgress && s.Client.DataAvailable)
                            {
                                s.ClientAsyncIoInProgress = true;
                                CopyStreamAsync(s.Client, s.Server, s.ClientSendBuffer, s.SetClientAsyncIoInProgressFalse);
                            }
                        }
                        catch { }
                    }

                    foreach (var s in localSessions)
                    {
                        try
                        {
                            if (!s.ServerAsyncIoInProgress && s.Server.DataAvailable)
                            {
                                s.ServerAsyncIoInProgress = true;
                                CopyStreamAsync(s.Server, s.Client, s.ServerSendBuffer, s.SetServerAsyncIoInProgressFalse);
                            }
                        }
                        catch { }
                    }
                }
            }
        }

        private void CopyStreamAsync(NetworkStream reader, NetworkStream writer, byte[] buffer, Action clearFlagAction)
        {
            var state = new Tuple<NetworkStream, byte[], Action>(writer, buffer, clearFlagAction);

            reader.ReadAsync(buffer, 0, buffer.Length).ContinueWith(OnReadCompleted, state);
        }

        private void OnReadCompleted(Task<int> readerTask, object objState)
        {
            var state = (Tuple<NetworkStream, byte[], Action>)objState;

            var writer = state.Item1;
            var buffer = state.Item2;
            var clearFlagAction = state.Item3;

            int nBytesRead = readerTask.Result;

            if (_logger != null)
            {
                try
                {
                    _logger(buffer, nBytesRead); // TODO: Async
                }
                catch { }
            }

            writer.WriteAsync(buffer, 0, nBytesRead).ContinueWith(OnWriteCompleted, clearFlagAction);
        }

        private static void OnWriteCompleted(Task t, object objState)
        {
            var clearIoInProgressAction = (Action)objState;

            clearIoInProgressAction();
        }
    }
}
