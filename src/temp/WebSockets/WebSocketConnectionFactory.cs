using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using vtortola.WebSockets;
using vtortola.WebSockets.Rfc6455;

namespace Bedrock.Framework.Transports.WebSockets;

public class WebSocketConnectionFactory : IConnectionFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public WebSocketConnectionFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public async ValueTask<ConnectionContext> ConnectAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
    {
        if (!(endpoint is UriEndPoint uriEndpoint))
        {
            throw new NotSupportedException($"{endpoint} is not supported");
        }

        var options = new WebSocketListenerOptions()
        {
            SendBufferSize = 8096, // 8kB
            BufferManager = BufferManager.CreateBufferManager(8096 * 100, 8096)
            //Logger = ??? // TODO: Implement
        };
        options.Standards.RegisterRfc6455();
        options.Transports.ConfigureTcp(configure =>
        {
            configure.BacklogSize = 100; // max pending connections waiting to be accepted
            configure.ReceiveBufferSize = 8096; // 8kB
            configure.SendBufferSize = 8096; // 8kB
        });

        var webSocketClient = new WebSocketClient(options);
        var webSocket = await webSocketClient.ConnectAsync(uriEndpoint.Uri, cancellationToken).ConfigureAwait(false);
        return new WebSocketConnectionContext(webSocket, options, _loggerFactory);
    }
}
