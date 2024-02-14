#if NETSTANDARD2_0
extern alias Backports;
using Backports::System.IO.Pipes;
#else
using System.IO.Pipes;
#endif

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Connections;

namespace Bedrock.Framework
{
    public class NamedPipeConnectionFactory : IConnectionFactory
    {
        public async ValueTask<ConnectionContext> ConnectAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            if (endpoint is not NamedPipeEndPoint np)
            {
                throw new NotSupportedException($"{endpoint.GetType()} is not supported");
            }

            var pipeStream = new NamedPipeClientStream(np.ServerName, np.PipeName, PipeDirection.InOut, np.PipeOptions, np.ImpersonationLevel);
            await pipeStream.ConnectAsync(cancellationToken).ConfigureAwait(false);

            return new NamedPipeConnectionContext(pipeStream, np);
        }
    }
}
