using Microsoft.AspNetCore.Connections;

namespace Bedrock.Framework.Transports.WebSockets;

public record WebSocketConnectionOptions(UriEndPoint Endpoint);