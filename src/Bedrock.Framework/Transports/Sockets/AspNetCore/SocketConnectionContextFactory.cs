#if NETSTANDARD

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

using System;
using System.Net.Sockets;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

/// <summary>
/// A factory for socket based connections contexts.
/// </summary>
public sealed class SocketConnectionContextFactory : IDisposable
{
    private readonly SocketConnectionFactoryOptions _options;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates the <see cref="SocketConnectionContextFactory"/>.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="logger">The logger.</param>
    public SocketConnectionContextFactory(SocketConnectionFactoryOptions options, ILogger logger)
    {
        ArgumentNullThrowHelper.ThrowIfNull(options);
        ArgumentNullThrowHelper.ThrowIfNull(logger);

        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Create a <see cref="ConnectionContext"/> for a socket.
    /// </summary>
    /// <param name="socket">The socket for the connection.</param>
    /// <returns></returns>
    public ConnectionContext Create(Socket socket)
    {
        var connection = new Bedrock.Framework.SocketConnection(socket, socket.LocalEndPoint);

        connection.Start();
        return connection;
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}

#endif