using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using vtortola.WebSockets;

namespace Bedrock.Framework.Transports.WebSockets;

public class WebSocketConnectionContext : ConnectionContext, IDuplexPipe
{
    private readonly Stream _stream;

    public PipeReader Input { get; }
    public PipeWriter Output { get; }
    public override IDuplexPipe Transport { get; set; }
    public override string ConnectionId { get; set; }
    public override IFeatureCollection Features { get; } = new FeatureCollection();
    public override IDictionary<object, object?> Items { get; set; } = new ConnectionItems();
    
    public WebSocketConnectionContext(WebSocket socket, WebSocketListenerOptions options, ILoggerFactory? loggerFactory = null)
    {
        _stream = new WebSocketStream(socket);
        Transport = this;
        ConnectionId = Guid.NewGuid().ToString();
        LocalEndPoint = socket.HttpRequest.LocalEndPoint;
        RemoteEndPoint = socket.HttpRequest.RemoteEndPoint;

        Input = PipeReader.Create(_stream);
        Output = PipeWriter.Create(_stream);
    }

    public override void Abort()
    {
        // TODO: Abort the socket. Do we dispose the Stream?
        base.Abort();
    }

#if (NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)
    public override ValueTask DisposeAsync()
    {
        Input.Complete();
        Output.Complete();

        _stream.Dispose();
        return default; // ValueTask.CompletedTask
    }
#else
    public override async ValueTask DisposeAsync()
    {
        Input.Complete();
        Output.Complete();

        await _stream.DisposeAsync();
    }
#endif
}

file class WebSocketStream : Stream
{
    private readonly WebSocket _socket;

    public WebSocketStream(WebSocket socket)
    {
        _socket = socket;
    }

    public override bool CanRead => _socket.IsConnected;
    public override bool CanSeek => false;
    public override bool CanWrite => _socket.IsConnected;
    public override long Length { get; }
    public override long Position { get; set; }

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _socket.WriteBytesAsync(buffer.AsSpan(offset, count).ToArray()).GetAwaiter().GetResult();
    }
}
