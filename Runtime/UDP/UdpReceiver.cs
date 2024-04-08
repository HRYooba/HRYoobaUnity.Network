using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using R3;

namespace HRYooba.Network.Udp
{
    /// <summary>
    /// UDP Sender
    /// </summary>
    public class UdpReceiver : IDisposable
    {
        private readonly UdpClient _client = null;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private bool _disposed = false;

        private readonly Subject<string> _onReceivedSubject = new();
        public Observable<string> OnReceivedObservable => _onReceivedSubject.ObserveOnMainThread();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="port"></param>
        public UdpReceiver(int port)
        {
            var endPoint = new IPEndPoint(IPAddress.Any, port);
            _client = new UdpClient(endPoint);

            Task.Run(() => ReceiveAsync(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// Destructor
        /// </summary>
        /// <returns></returns>
        ~UdpReceiver() => Dispose();

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _client.Dispose();
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _onReceivedSubject.Dispose();
        }

        /// <summary>
        /// ReceiveAsync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task ReceiveAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _client.ReceiveAsync();
                    cancellationToken.ThrowIfCancellationRequested();

                    var message = Encoding.UTF8.GetString(result.Buffer);
                    _onReceivedSubject.OnNext(message);
                }
                catch
                {
                    throw;
                }
            }
        }
    }
}