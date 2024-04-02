using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using R3;

namespace HRYooba.Network.Udp
{
    public class UdpSender
    {
        private readonly UdpClient _client = null;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private bool _disposed = false;

        private readonly Subject<string> _onSendSubject = new();
        public Observable<string> OnSendObservable => _onSendSubject.ObserveOnMainThread();

        public UdpSender(string ipAddress, int port)
        {
            _client = new UdpClient();
            _client.Connect(ipAddress, port);
        }

        ~UdpSender() => Dispose();

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _client.Dispose();
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
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
            
            var bytes = Encoding.UTF8.GetBytes(message);
            await _client.SendAsync(bytes, bytes.Length);
            cancellationToken.ThrowIfCancellationRequested();

            _onSendSubject.OnNext(message);
        }
    }
}