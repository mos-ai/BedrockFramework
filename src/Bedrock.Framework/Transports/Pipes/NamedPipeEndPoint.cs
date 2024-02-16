#if NETSTANDARD2_0
using Backports.System.IO.Pipes;
#else
using System.IO.Pipes;
#endif

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Principal;

namespace Bedrock.Framework
{
    public class NamedPipeEndPoint : EndPoint
    {
        public NamedPipeEndPoint(string pipeName,
                                 string serverName = ".",
                                 PipeOptions pipeOptions = PipeOptions.WriteThrough | PipeOptions.Asynchronous,
                                 TokenImpersonationLevel impersonationLevel = TokenImpersonationLevel.Anonymous)
        {
            ServerName = serverName;
            PipeName = pipeName;
            PipeOptions = pipeOptions;
            ImpersonationLevel = impersonationLevel;
        }

        public string ServerName { get; }
        public string PipeName { get; }
        public PipeOptions PipeOptions { get; set; }
        public TokenImpersonationLevel ImpersonationLevel { get; set; }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is NamedPipeEndPoint other && other.ServerName == ServerName && other.PipeName == PipeName;
        }

        public override int GetHashCode()
        {
            return ServerName.GetHashCode() ^ PipeName.GetHashCode();
        }

        public override string ToString()
        {
            return $"Server = {ServerName}, Pipe = {PipeName}";
        }
    }
}
