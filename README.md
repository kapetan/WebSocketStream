# WebSocketStream

Stream interface implemented on top of `System.Net.WebSockets`.

    dotnet add package WebSocketStream

# Usage

Client example.

```C#
using WSStream;

WebSocketStream socket = WebSocketStream.Connect("ws://localhost:8080");
await socket.WriteAsync(new byte[] { 100, 101 }, 0, 2);

byte[] buffer = new buffer[1024];
int read = await socket.ReadAsync(buffer, 0, buffer.Length);
await socket.CloseAsync();
```

The server uses a `HttpListener` under the hood for listening to incoming requests.

```C#
using WSStream;

WebSocketListener server = new WebSocketListener(8080);
WebSocketStream socket = await server.AcceptWebSocket();

byte[] buffer = new byte[1024];
int read = await socket.ReadAsync(buffer, 0, buffer.Length);
await socket.WriteAsync(buffer, 0, read);
await socket.CloseAsync();
```

Since `System.Net.WebSockets` does not provide synchronous methods for reading and writing to a WebSocket, the stream implementation uses the asynchronous equivalents in `Read(buffer, offset, count)` and `Write(buffer, offset, count)` and blocks the thread by calling `GetAwaiter().GetResult()`.
