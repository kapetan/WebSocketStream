using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.WebSockets;

namespace WSStream {
    public class WebSocketServer : IDisposable {
        public event Func<object, RequestedEventArgs, Task> Requested;
        public event Func<object, ConnectedEventArgs, Task> Connected;

        private HttpListener http;

        public Task Listen(int port, string host = null) {
            HttpListener http = new HttpListener();
            host = host ?? "+";
            http.Prefixes.Add($"http://{host}:{port}/");
            return Listen(http);
        }

        public async Task Listen(HttpListener http) {
            this.http = http;
            http.Start();

            try {
                while (true) {
                    HttpListenerContext httpContext = await http.GetContextAsync();

                    if (httpContext.Request.IsWebSocketRequest) {
                        await OnEvent(Requested, new RequestedEventArgs(httpContext.Request));
                        WebSocketContext websocketContext = await httpContext.AcceptWebSocketAsync(null);
                        WebSocketStream stream = new WebSocketStream(websocketContext.WebSocket);
                        await OnEvent(Connected, new ConnectedEventArgs(stream));
                    } else {
                        httpContext.Response.StatusCode = 400;
                        httpContext.Response.Close();
                    }
                }
            } catch (ObjectDisposedException) {
                // Thrown when server is closed while awaiting
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

        private async Task OnEvent<T>(Func<object, T, Task> handler, T e) {
            if (handler != null) {
                foreach (Func<object, T, Task> fn in handler.GetInvocationList()) {
                    await fn(this, e);
                }
            }
        }

        public class RequestedEventArgs : EventArgs {
            public RequestedEventArgs(HttpListenerRequest request) {
                Request = request;
            }

            public HttpListenerRequest Request {
                get;
                private set;
            }
        }

        public class ConnectedEventArgs : EventArgs {
            public ConnectedEventArgs(WebSocketStream stream) {
                Stream = stream;
            }

            public WebSocketStream Stream {
                get;
                private set;
            }
        }
    }
}
