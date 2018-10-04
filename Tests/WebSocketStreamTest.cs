using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace WSStream.Tests {
    public class WebSocketStreamTest {
        private int PORT = 8080;

        [Fact]
        public async Task BasicClientServerTest() {
            WebSocketListener server = new WebSocketListener(PORT);
            byte[] message = new byte[] { 100, 101, 102, 103 };

            server.Start();

            Task task = Task.Run(async () => {
                Stream socket = await server.AcceptWebSocketAsync();

                byte[] buffer = new byte[1024];
                int read = await socket.ReadAsync(buffer, 0, buffer.Length);

                Assert.Equal(4, read);
                Assert.Equal(message, buffer.Take(4).ToArray());
            });

            Stream client = WebSocketStream.Connect($"ws://localhost:{PORT}");

            await client.WriteAsync(message, 0, message.Length);

            client.Dispose();
            server.Dispose();

            await task;
        }

        [Fact]
        public async Task ClientServerTest() {
            WebSocketListener server = new WebSocketListener(PORT);
            byte[] clientMessage = new byte[] { 100, 101, 102, 103 };
            byte[] serverMessage = new byte[] { 80, 81, 82, 83 };
            byte[] clientBuffer = new byte[1024];

            server.Start();

            Task task = Task.Run(async () => {
                WebSocketStream socket = await server.AcceptWebSocketAsync();
                byte[] serverBuffer = new byte[1024];

                int serverRead = await socket.ReadAsync(serverBuffer, 0, serverBuffer.Length);

                Assert.Equal(4, serverRead);
                Assert.Equal(clientMessage, serverBuffer.Take(4).ToArray());

                await socket.WriteAsync(serverMessage, 0, serverMessage.Length);

                serverRead = await socket.ReadAsync(serverBuffer, 0, serverBuffer.Length);

                Assert.Equal(0, serverRead);
            });

            WebSocketStream client = WebSocketStream.Connect($"ws://localhost:{PORT}");

            await client.WriteAsync(clientMessage, 0, clientMessage.Length);
            int clientRead = await client.ReadAsync(clientBuffer, 0, clientBuffer.Length);

            Assert.Equal(4, clientRead);
            Assert.Equal(serverMessage, clientBuffer.Take(4).ToArray());

            await client.CloseAsync();
            client.Dispose();
            server.Dispose();

            await task;
        }

        [Fact]
        public async Task CancelServerAcceptTest() {
            WebSocketListener server = new WebSocketListener(PORT);
            CancellationTokenSource source = new CancellationTokenSource();

            server.Start();
            Task<WebSocketStream> task = server.AcceptWebSocketAsync(source.Token);

            source.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => task);
        }

        [Fact]
        public async Task DisposeServerTest() {
            WebSocketListener server = new WebSocketListener(PORT);

            server.Start();
            Task<WebSocketStream> task = server.AcceptWebSocketAsync();
            server.Dispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => task);
        }
    }
}
