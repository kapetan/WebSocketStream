using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;

namespace WSStream {
    public class WebSocketClientStream : WebSocketStream {
        private Uri uri;
        private Task connect;
        private object mutex = new object();

        public WebSocketClientStream(Uri uri) : base(new ClientWebSocket()) {
            this.uri = uri;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            await ConnectOnce(cancellationToken);
            return await base.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            await ConnectOnce(cancellationToken);
            await base.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public Task ConnectAsync() {
            return ConnectAsync(CancellationToken.None);
        }

        public Task ConnectAsync(CancellationToken cancellationToken) {
            return ConnectOnce(cancellationToken);
        }

        private Task ConnectOnce(CancellationToken cancellationToken) {
            if (connect == null) {
                lock (mutex) {
                    if (connect == null) connect = Connect(cancellationToken);
                }
            }

            return connect;
        }

        private async Task Connect(CancellationToken cancellationToken) {
            if (socket == null) throw new ObjectDisposedException("WebSocketStream");
            await ((ClientWebSocket) socket).ConnectAsync(uri, cancellationToken);
        }
    }
}
