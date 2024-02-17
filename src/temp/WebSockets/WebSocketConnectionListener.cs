using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using vtortola.WebSockets;

namespace Bedrock.Framework.Transports.WebSockets;
public class WebSocketConnectionListener : IConnectionListener
{
    private readonly WebSocketListener _server;
    private readonly Channel<ConnectionContext> _acceptQueue = Channel.CreateUnbounded<ConnectionContext>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public EndPoint EndPoint { get; set; }

    public WebSocketConnectionListener(Action<WebSocketListenerOptions> configure, IServiceProvider serviceProvider, EndPoint endPoint)
    {
        if (endPoint is UriEndPoint uriEndpoint)
        {
            _server = new WebSocketListener(new Uri[] { uriEndpoint.Uri }, new WebSocketListenerOptions());
        }
        else if (endPoint is IPEndPoint iPEndpoint)
        {
            _server = new WebSocketListener(iPEndpoint);
        }
        else
        {
            ArgumentThrowHelper.Throw(nameof(endPoint));
        }

        _server.MapConnections(path, options, cb => cb.Run(inner =>
        {
            var connection = new WebSocketHostConnectionContext(inner);
            _acceptQueue.Writer.TryWrite(connection);
            return connection.ExecutionTask;
        }));
    }

    public async Task BindAsync(CancellationToken cancellationToken)
    {
        await _server.StartAsync();
    }

    public async ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
    {
        while (await _acceptQueue.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_acceptQueue.Reader.TryRead(out var connection))
            {
                return connection;
            }
        }
        return null;
    }

    public async ValueTask DisposeAsync()
    {
        await UnbindAsync().ConfigureAwait(false);

        await _server.StopAsync();
    }

    public async ValueTask UnbindAsync(CancellationToken cancellationToken = default)
    {
        await _server.StopAsync();

        _acceptQueue.Writer.TryComplete();
    }
}

// This exists solely to track the lifetime of the connection
file class WebSocketHostConnectionContext : ConnectionContext
{
    private readonly ConnectionContext _connection;
    private readonly TaskCompletionSource<object> _executionTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

    public WebSocketHostConnectionContext(ConnectionContext connection)
    {
        _connection = connection;
    }

    public Task ExecutionTask => _executionTcs.Task;

    public override string ConnectionId
    {
        get => _connection.ConnectionId;
        set => _connection.ConnectionId = value;
    }

    public override IFeatureCollection Features => _connection.Features;

    public override IDictionary<object, object> Items
    {
        get => _connection.Items;
        set => _connection.Items = value;
    }

    public override IDuplexPipe Transport
    {
        get => _connection.Transport;
        set => _connection.Transport = value;
    }

    public override EndPoint LocalEndPoint
    {
        get => _connection.LocalEndPoint;
        set => _connection.LocalEndPoint = value;
    }

    public override EndPoint RemoteEndPoint
    {
        get => _connection.RemoteEndPoint;
        set => _connection.RemoteEndPoint = value;
    }

    public override CancellationToken ConnectionClosed
    {
        get => _connection.ConnectionClosed;
        set => _connection.ConnectionClosed = value;
    }

    public override void Abort()
    {
        _connection.Abort();
    }

    public override void Abort(ConnectionAbortedException abortReason)
    {
        _connection.Abort(abortReason);
    }

    public override ValueTask DisposeAsync()
    {
        _executionTcs.TrySetResult(null);
        return _connection.DisposeAsync();
    }
}
