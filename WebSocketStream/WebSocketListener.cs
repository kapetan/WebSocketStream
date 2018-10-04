using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.WebSockets;

namespace WSStream {
    public class WebSocketListener : IDisposable {
        private HttpListener http;

        public WebSocketListener(int port, string host = null) {
            HttpListener http = new HttpListener();
            host = host ?? "+";
            http.Prefixes.Add($"http://{host}:{port}/");
            this.http = http;
        }

        public WebSocketListener(HttpListener http) {
            this.http = http;
        }

        public void Start() {
            ThrowIfDisposed();
            http.Start();
        }

        public void Stop() {
            ThrowIfDisposed();
            http.Stop();
        }

        public Task<WebSocketStream> AcceptWebSocketAsync() {
            return AcceptWebSocketAsync(CancellationToken.None);
        }

        public async Task<WebSocketStream> AcceptWebSocketAsync(CancellationToken cancellationToken) {
            ThrowIfDisposed();

            try {
                using (cancellationToken.Register(Stop)) {
                    while (true) {
                        HttpListenerContext httpContext = await http.GetContextAsync();

                        if (httpContext.Request.IsWebSocketRequest) {
                            WebSocketContext websocketContext = await httpContext.AcceptWebSocketAsync(null);
                            return new WebSocketStream(websocketContext.WebSocket);
                        } else {
                            httpContext.Response.StatusCode = 400;
                            httpContext.Response.Close();
                        }
                    }
                }
            } catch (ObjectDisposedException e) when (cancellationToken.IsCancellationRequested) {
                if (http == null) throw e;
                else Dispose();
                throw new OperationCanceledException("The operation was cancelled", e, cancellationToken);
            }
        }

        public void Dispose() {
            Dispose(true);
        }

        protected void Dispose(bool disposing) {
            try {
                if (disposing && http != null) http.Close();
            } finally {
                http = null;
            }
        }

        private void ThrowIfDisposed() {
            if (http == null) throw new ObjectDisposedException("http");
        }
    }
}
