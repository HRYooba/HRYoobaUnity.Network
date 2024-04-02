using System;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using R3;

namespace HRYooba.Network.Tcp
{
    public class TcpSender : IDisposable
    {
        private readonly string _ipAddress = null;
        private readonly int _port = 0;
        private readonly TcpClient _client = null;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private bool _disposed = false;

        private readonly Subject<(string IpAddress, int Port)> _onConnectedSubject = new();
        private readonly Subject<(string IpAddress, int Port)> _onDisconnectedSubject = new();
        private readonly Subject<string> _onSendSubject = new();

        public Observable<(string IpAddress, int Port)> OnConnectedObservable => _onConnectedSubject.ObserveOnMainThread();
        public Observable<(string IpAddress, int Port)> OnDisconnectedObservable => _onDisconnectedSubject.ObserveOnMainThread();
        public Observable<string> OnSendObservable => _onSendSubject.ObserveOnMainThread();

        public TcpSender(string ipAddress, int port)
        {
            _client = new TcpClient();
            _ipAddress = ipAddress;
            _port = port;

            _ = ConnectAsync(_cancellationTokenSource.Token);
        }

        ~TcpSender() => Dispose();

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _client.Dispose();
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _onConnectedSubject.Dispose();
            _onDisconnectedSubject.Dispose();
            _onSendSubject.Dispose();
        }

        public void Send(string message)
        {
            if (_disposed) return;
            _ = SendAsync(message, _cancellationTokenSource.Token);
        }

        public async Task SendAsync(string message, CancellationToken cancellationToken)
        {
            if (_disposed) return;
            if (!IsConnecting()) return;

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

        private async Task ConnectAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _client.ConnectAsync(_ipAddress, _port);
                cancellationToken.ThrowIfCancellationRequested();
                if (!_disposed) _onConnectedSubject.OnNext((_ipAddress, _port));
            }
            catch (SocketException)
            {
                if (!_disposed)
                {
                    _onDisconnectedSubject.OnNext((_ipAddress, _port));
                }
            }
            catch
            {
                throw;
            }
        }

        private async Task WriteAsync(Stream stream, string message, CancellationToken cancellationToken)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            try
            {
                await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!_disposed) _onSendSubject.OnNext(message);
            }
            catch
            {
                throw;
            }
        }
    }
}