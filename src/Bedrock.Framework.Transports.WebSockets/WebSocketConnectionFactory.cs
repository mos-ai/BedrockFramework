using Bedrock.Framework.Transports.WebSockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Bedrock.Framework.Transports
{
    public class WebSocketConnectionFactory : IConnectionFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        public WebSocketConnectionFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public ValueTask<ConnectionContext> ConnectAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            return new WebsocketConnection(endpoint).StartAsync();

        }
    }
}
