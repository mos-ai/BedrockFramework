using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using vtortola.WebSockets;

namespace Bedrock.Framework.Transports.WebSockets;

public class WebSocketConnectionListenerFactory : IConnectionListenerFactory, IHostApplicationLifetime
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly Action<WebSocketListenerOptions> _configure;

    public WebSocketConnectionListenerFactory(ILoggerFactory loggerFactory, Action<WebSocketListenerOptions>? configure = null)
    {
        _loggerFactory = loggerFactory;
        _configure = configure ?? new Action<WebSocketListenerOptions>(_ => { });
    }

    public CancellationToken ApplicationStarted => default;

    public CancellationToken ApplicationStopped => default;

    public CancellationToken ApplicationStopping => default;

    public async ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void StopApplication()
    {
        // Shut the server down
    }
}
