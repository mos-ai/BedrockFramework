﻿using System;

namespace Bedrock.Framework
{
    public static partial class ServerBuilderExtensions
    {
        public static ServerBuilder UseSockets(this ServerBuilder serverBuilder, Action<SocketsServerBuilder> configure)
        {
            var socketsBuilder = new SocketsServerBuilder();
            configure(socketsBuilder);
            socketsBuilder.Apply(serverBuilder);
            return serverBuilder;
        }

        public static ClientBuilder UseSockets(this ClientBuilder clientBuilder)
        {
            return clientBuilder.UseConnectionFactory(new SocketConnectionFactory());
        }
    }
}