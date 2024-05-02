using System;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using R3;

namespace HRYooba.Network.Udp
{
    /// <summary>
    /// UDP Sender
    /// </summary>
    public class UdpSender : IDisposable
    {
        private readonly UdpClient _client = null;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private bool _disposed = false;

        private readonly Subject<string> _onSendSubject = new();
        public Observable<string> OnSendObservable => _onSendSubject.ObserveOnMainThread();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="port"></param>
        public UdpSender(string ipAddress, int port)
        {
            _client = new UdpClient();
            _client.Connect(ipAddress, port);
        }

        /// <summary>
        /// Destructor
        /// </summary>
        /// <returns></returns>
        ~UdpSender() => Dispose();

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

            var bytes = Encoding.UTF8.GetBytes(message);
            await _client.SendAsync(bytes, bytes.Length);
            cancellationToken.ThrowIfCancellationRequested();

            _onSendSubject.OnNext(message);
        }
    }
}