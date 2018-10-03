using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace WebSocketStream.Tests {
    public class WebSocketStreamTest {
        private int PORT = 8080;

        [Fact]
        public async Task ClientServerTest() {
            WebSocketServer server = new WebSocketServer();
            WebSocketServer.RequestedEventArgs requested = null;
            WebSocketServer.ConnectedEventArgs connected = null;
            byte[] clientMessage = new byte[] { 100, 101, 102, 103 };
            byte[] serverMessage = new byte[] { 80, 81, 82, 83 };
            byte[] clientBuffer = new byte[1024];

            server.Requested += (sender, e) => {
                requested = e;
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

                connected = e;
            };

            Task listenTask = server.Listen(PORT);
            WebSocketStream client = WebSocketStream.Connect($"ws://localhost:{PORT}");

            await client.WriteAsync(clientMessage, 0, clientMessage.Length);
            int clientRead = await client.ReadAsync(clientBuffer, 0, clientBuffer.Length);

            Assert.Equal(4, clientRead);
            Assert.Equal(serverMessage, clientBuffer.Take(4).ToArray());

            await client.CloseAsync();

            Assert.NotNull(requested);
            Assert.NotNull(connected);

            server.Dispose();
            await listenTask;
        }
    }
}
