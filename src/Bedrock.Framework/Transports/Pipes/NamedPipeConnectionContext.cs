﻿#if NETSTANDARD2_0
using Backports.System.IO.Pipes;
#else
using System.IO.Pipes;
#endif

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Bedrock.Framework
{
    internal class NamedPipeConnectionContext : ConnectionContext, IDuplexPipe
    {
        private readonly PipeStream _stream;

        public NamedPipeConnectionContext(PipeStream stream, NamedPipeEndPoint endPoint)
        {
            _stream = stream;
            Transport = this;
            ConnectionId = Guid.NewGuid().ToString();
            RemoteEndPoint = endPoint;
            
            Input = PipeReader.Create(stream);
            Output = PipeWriter.Create(stream);
        }

        public PipeReader Input { get; }

        public PipeWriter Output { get; }
        public override string ConnectionId { get; set; }

        public override IFeatureCollection Features { get; } = new FeatureCollection();

        public override IDictionary<object, object?> Items { get; set; } = new ConnectionItems();
        public override IDuplexPipe Transport { get; set; }

        public override void Abort()
        {
            // TODO: Abort the named pipe. Do we dispose the Stream?
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
}
