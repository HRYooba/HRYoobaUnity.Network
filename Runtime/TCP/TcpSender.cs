using System;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using R3;

namespace HRYooba.Network.Tcp
{
    /// <summary>
    /// TCP Sender
    /// </summary>
    public class TcpSender : IDisposable
    {
        private readonly string _ipAddress = null;
        private readonly int _port = 0;
        private readonly int _timeout = 3000;
        private readonly TcpClient _client = null;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private bool _disposed = false;

        private readonly Subject<(string IpAddress, int Port)> _onConnectedSubject = new();
        private readonly Subject<(string IpAddress, int Port)> _onConnectionFailedSubject = new();
        private readonly Subject<(string IpAddress, int Port)> _onDisconnectedSubject = new();
        private readonly Subject<string> _onSendSubject = new();

        public Observable<(string IpAddress, int Port)> OnConnectedObservable => _onConnectedSubject.ObserveOnMainThread();
        public Observable<(string IpAddress, int Port)> OnConnectionFailedObservable => _onConnectionFailedSubject.ObserveOnMainThread();
        public Observable<(string IpAddress, int Port)> OnDisconnectedObservable => _onDisconnectedSubject.Take(1).ObserveOnMainThread();
        public Observable<string> OnSendObservable => _onSendSubject.ObserveOnMainThread();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="port"></param>
        /// <param name="conectTimeout">Milliseconds</param>
        public TcpSender(string ipAddress, int port, int conectTimeout = 3000)
        {
            _client = new TcpClient();
            _ipAddress = ipAddress;
            _port = port;
            _timeout = conectTimeout;

            _ = ConnectAsync(_cancellationTokenSource.Token);
        }

        /// <summary>
        /// Destructor
        /// </summary>
        /// <returns></returns>
        ~TcpSender() => Dispose();

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_client.Connected) _onDisconnectedSubject.OnNext((_ipAddress, _port));
            _client.Dispose();
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _onConnectedSubject.Dispose();
            _onConnectionFailedSubject.Dispose();
            _onDisconnectedSubject.Dispose();
            _onSendSubject.Dispose();
        }

        /// <summary>
        /// Send
        /// </summary>
        /// <param name="message"></param>
        public void Send(string message)
        {
            if (_disposed) return;
            _ = SendAsync(message, _cancellationTokenSource.Token);
        }

        /// <summary>
        /// SendAsync
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task SendAsync(string message, CancellationToken cancellationToken)
        {
            if (_disposed) return;

            var connectFailedTask = _onConnectionFailedSubject.FirstAsync(cancellationToken);
            while (!_client.Connected)
            {
                if (connectFailedTask.IsCompleted) throw new Exception("[TcpSender] SendAsync: Connection failed.");
                await Task.Delay(100, cancellationToken);
            }

            try
            {
                await WriteAsync(_client.GetStream(), message, cancellationToken);

                if (!IsConnecting())
                {
                    _onDisconnectedSubject.OnNext((_ipAddress, _port));
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// IsConnecting
        /// </summary>
        /// <returns></returns>
        private bool IsConnecting()
        {
            try
            {
                return !(_client.Client.Poll(0, SelectMode.SelectRead) && _client.Client.Available == 0);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// ConnectAsync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task ConnectAsync(CancellationToken cancellationToken)
        {
            try
            {
                var connectTask = _client.ConnectAsync(_ipAddress, _port);
                var timeoutTask = Task.Delay(_timeout, cancellationToken);
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                cancellationToken.ThrowIfCancellationRequested();

                if (completedTask == timeoutTask)
                {
                    _onConnectionFailedSubject.OnNext((_ipAddress, _port));
                }
                else
                {
                    if (_client.Connected)
                    {
                        _onConnectedSubject.OnNext((_ipAddress, _port));
                    }
                    else
                    {
                        _onConnectionFailedSubject.OnNext((_ipAddress, _port));
                    }
                }
            }
            catch
            {
                _onConnectionFailedSubject.OnNext((_ipAddress, _port));
                throw;
            }
        }

        /// <summary>
        /// WriteAsync
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task WriteAsync(Stream stream, string message, CancellationToken cancellationToken)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            try
            {
                await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                _onSendSubject.OnNext(message);
            }
            catch
            {
                throw;
            }
        }
    }
}