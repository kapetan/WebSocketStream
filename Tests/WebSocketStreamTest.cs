using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace WSStream.Tests {
    public class WebSocketStreamTest {
        private int PORT = 8080;

        [Fact]
        public async Task BasicClientServerTest() {
            WebSocketServer server = new WebSocketServer();
            TaskCompletionSource<WebSocketServer.ConnectedEventArgs> connected =
                new TaskCompletionSource<WebSocketServer.ConnectedEventArgs>();
            byte[] message = new byte[] { 100, 101, 102, 103 };

            server.Connected += async (sender, e) => {
                Stream socket = e.Stream;
                byte[] buffer = new byte[1024];

                int read = await socket.ReadAsync(buffer, 0, buffer.Length);

                Assert.Equal(4, read);
                Assert.Equal(message, buffer.Take(4).ToArray());

                connected.SetResult(e);
            };

            Task listenTask = server.Listen(PORT);
            Stream client = WebSocketStream.Connect($"ws://localhost:{PORT}");

            await client.WriteAsync(message, 0, message.Length);
            WebSocketServer.ConnectedEventArgs args = await connected.Task;

            Assert.NotNull(args);

            client.Dispose();
            server.Dispose();

            await listenTask;
        }

        [Fact]
        public async Task ClientServerTest() {
            WebSocketServer server = new WebSocketServer();
            TaskCompletionSource<WebSocketServer.RequestedEventArgs> requested =
                new TaskCompletionSource<WebSocketServer.RequestedEventArgs>();
            TaskCompletionSource<WebSocketServer.ConnectedEventArgs> connected =
                new TaskCompletionSource<WebSocketServer.ConnectedEventArgs>();
            byte[] clientMessage = new byte[] { 100, 101, 102, 103 };
            byte[] serverMessage = new byte[] { 80, 81, 82, 83 };
            byte[] clientBuffer = new byte[1024];

            server.Requested += (sender, e) => {
                requested.SetResult(e);
                return Task.CompletedTask;
            };

            server.Connected += async (sender, e) => {
                WebSocketStream socket = e.Stream;
                byte[] serverBuffer = new byte[1024];

                int serverRead = await socket.ReadAsync(serverBuffer, 0, serverBuffer.Length);

                Assert.Equal(4, serverRead);
                Assert.Equal(clientMessage, serverBuffer.Take(4).ToArray());

                await socket.WriteAsync(serverMessage, 0, serverMessage.Length);

                serverRead = await socket.ReadAsync(serverBuffer, 0, serverBuffer.Length);

                Assert.Equal(0, serverRead);

                connected.SetResult(e);
            };

            Task listenTask = server.Listen(PORT);
            WebSocketStream client = WebSocketStream.Connect($"ws://localhost:{PORT}");

            await client.WriteAsync(clientMessage, 0, clientMessage.Length);
            int clientRead = await client.ReadAsync(clientBuffer, 0, clientBuffer.Length);

            Assert.Equal(4, clientRead);
            Assert.Equal(serverMessage, clientBuffer.Take(4).ToArray());

            await client.CloseAsync();

            WebSocketServer.RequestedEventArgs requestedArgs = await requested.Task;
            WebSocketServer.ConnectedEventArgs connectedArgs = await connected.Task;

            Assert.NotNull(requestedArgs);
            Assert.NotNull(connectedArgs);

            server.Dispose();
            await listenTask;
        }
    }
}
