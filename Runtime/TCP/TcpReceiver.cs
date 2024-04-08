using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using R3;

namespace HRYooba.Network.Tcp
{
    /// <summary>
    /// TCP Receiver
    /// </summary>
    public class TcpReceiver : IDisposable
    {
        private readonly int _bufferSize = 1024;
        private readonly TcpListener _listener = null;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private bool _disposed = false;

        private readonly Subject<string> _onConnectedSubject = new();
        private readonly Subject<string> _OnDisconnectedSubject = new();
        private readonly Subject<string> _onReceivedSubject = new();
        
        public Observable<string> OnConnectedObservable => _onConnectedSubject.ObserveOnMainThread();
        public Observable<string> OnDisconnectedObservable => _OnDisconnectedSubject.ObserveOnMainThread();
        public Observable<string> OnReceivedObservable => _onReceivedSubject.ObserveOnMainThread();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="port"></param>
        /// <param name="bufferSize"></param>
        public TcpReceiver(int port, int bufferSize = 1024)
        {
            _bufferSize = bufferSize;
            _listener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
            _listener.Start();

            Task.Run(() => AcceptClientsAsync(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// Destructor
        /// </summary>
        /// <returns></returns>
        ~TcpReceiver() => Dispose();

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _listener.Stop();
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _onConnectedSubject.Dispose();
            _OnDisconnectedSubject.Dispose();
            _onReceivedSubject.Dispose();
        }

        /// <summary>
        /// AcceptClientsAsync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    cancellationToken.ThrowIfCancellationRequested();
                    _onConnectedSubject.OnNext(client.ToIPAddressString());

                    _ = ReceiveAsync(client, cancellationToken);
                }
                catch (ObjectDisposedException)
                {

                }
                catch
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// ReceiveAsync
        /// </summary>
        /// <param name="client"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task ReceiveAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using (client)
            {
                var stream = client.GetStream();

                while (!cancellationToken.IsCancellationRequested)
                {
                    var result = await ReadAsync(stream, cancellationToken);
                    if (!result) break;
                }

                // 通信切断されたとき
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    _OnDisconnectedSubject.OnNext(client.ToIPAddressString());
                }
                catch
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// ReadAsync
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<bool> ReadAsync(Stream stream, CancellationToken cancellationToken)
        {
            var dataBuffer = new StringBuilder();

            while (!cancellationToken.IsCancellationRequested)
            {
                var bytes = new byte[_bufferSize];
                var bytesSize = await stream.ReadAsync(bytes, 0, _bufferSize, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                if (bytesSize > 0)
                {
                    // dataを受信したらbufferにためる
                    var data = Encoding.UTF8.GetString(bytes, 0, bytesSize);
                    dataBuffer.Append(data);
                }
                else
                {
                    // 途中で通信切断された場合
                    return false;
                }

                cancellationToken.ThrowIfCancellationRequested();
                var message = dataBuffer.ToString();
                _onReceivedSubject.OnNext(message);
                return true;
            }

            return false;
        }
    }
}