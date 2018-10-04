using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.WebSockets;

namespace WSStream {
    public class WebSocketStream : Stream {
        public static WebSocketStream Connect(string uri) {
            return Connect(new Uri(uri));
        }

        public static WebSocketStream Connect(Uri uri) {
            return new WebSocketClientStream(uri);
        }

        protected WebSocket socket;

        public WebSocketStream(WebSocket socket) {
            this.socket = socket;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();

        public override long Position {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            ThrowIfDisposed();

            if (socket.State == WebSocketState.Closed ||
                    socket.State == WebSocketState.CloseReceived ||
                    socket.State == WebSocketState.CloseSent) {
                return 0;
            }

            ArraySegment<byte> segment = new ArraySegment<byte>(buffer, offset, count);
            WebSocketReceiveResult result = await socket.ReceiveAsync(segment, cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close) {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Remote close", cancellationToken);
                return 0;
            }

            return result.Count;
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            ThrowIfDisposed();
            if (socket.State != WebSocketState.Open) throw new IOException($"WebSocket not open ({socket.State})");

            ArraySegment<byte> segment = new ArraySegment<byte>(buffer, offset, count);
            await socket.SendAsync(segment, WebSocketMessageType.Binary, true, cancellationToken);
        }

        public Task CloseAsync() {
            return CloseAsync(CancellationToken.None);
        }

        public Task CloseAsync(CancellationToken cancellationToken) {
            ThrowIfDisposed();
            if (socket.State == WebSocketState.Closed) return Task.CompletedTask;
            return socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Local close", cancellationToken);
        }

        public override void Flush() {}

        public override int Read(byte[] buffer, int offset, int count) {
            return ReadAsync(buffer, offset, count).GetAwaiter().GetResult();
        }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotSupportedException();
        }

        public override void SetLength(long value) {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count) {
            WriteAsync(buffer, offset, count).GetAwaiter().GetResult();
        }

        protected override void Dispose(bool disposing) {
            try {
                if (disposing && socket != null) socket.Dispose();
            } finally {
                socket = null;
                base.Dispose(disposing);
            }
        }

        private void ThrowIfDisposed() {
            if (socket == null) throw new ObjectDisposedException("socket");
        }
    }
}
