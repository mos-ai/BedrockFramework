using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using System.Threading.Tasks;
using System.IO;
using System.Buffers;

namespace Bedrock.Framework.Transports.WebSockets
{
    internal class WebsocketConnection : ConnectionContext, IConnectionInherentKeepAliveFeature
    {

        public override string ConnectionId { get; set; } = Guid.NewGuid().ToString();

        public override IDuplexPipe Transport { get; set; }

        public override IFeatureCollection Features { get; } = new FeatureCollection();

        public override IDictionary<object, object> Items { get; set; } = new ConnectionItems();

        public bool HasInherentKeepAlive { get; } = true;
        private WebSocketSharp.WebSocket _ws { get; set; }
        private IDuplexPipe _application;
        public WebsocketConnection(EndPoint endPoint)
        {
            _ws = new WebSocketSharp.WebSocket(endPoint.ToString());
            var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);

            Features.Set<IConnectionInherentKeepAliveFeature>(this);
        }
        public async ValueTask<ConnectionContext> StartAsync()
        {
            _ws.Connect();

            //await _socket.ConnectAsync(_endPoint).ConfigureAwait(false);

            var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);

            //LocalEndPoint = ws.Origin;
            //RemoteEndPoint = _socket.RemoteEndPoint;

            Transport = pair.Transport;
            _application = pair.Application;

            _ws.OnMessage += async(sender, e) =>
            {
                var buffer = _application.Output.GetMemory();
                e.RawData.CopyTo(buffer);
                _application.Output.Advance(buffer.Length);
                var flushTask = _application.Output.FlushAsync();

                if (!flushTask.IsCompleted)
                {
                    await flushTask.ConfigureAwait(false);
                }

                var result = flushTask.Result;
                if (result.IsCompleted)
                {
                    // Pipe consumer is shut down, do we stop writing
                    // TODO: Kill app?
                    //break;
                }
            };

            //_ = ExecuteAsync();

            return this;
        }

        private async Task ExecuteAsync()
        {
            Exception sendError = null;
            try
            {
                // Spawn send and receive logic
                //var receiveTask = DoReceive();
                var sendTask = DoSend();

                // If the sending task completes then close the receive
                // We don't need to do this in the other direction because the kestrel
                // will trigger the output closing once the input is complete.
                if (await Task.WhenAny(sendTask).ConfigureAwait(false) == sendTask)
                {
                    // Tell the reader it's being aborted
                    _ws.Close();
                }

                // Now wait for sendTask to complete
                sendError = await sendTask;

                // Dispose the socket(should noop if already called)
                //_socket.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected exception in {nameof(WebsocketConnection)}.{nameof(StartAsync)}: " + ex);
            }
            finally
            {
                // Complete the output after disposing the socket
                _application.Input.Complete(sendError);
            }
        }
        public override async ValueTask DisposeAsync()
        {
            if (Transport != null)
            {
                await Transport.Output.CompleteAsync().ConfigureAwait(false);
                await Transport.Input.CompleteAsync().ConfigureAwait(false);
            }

            // Completing these loops will cause ExecuteAsync to Dispose the socket.
        }
        private async Task<Exception> DoSend()
        {
            Exception error = null;

            try
            {
                await ProcessSends().ConfigureAwait(false);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
            {
                error = null;
            }
            catch (ObjectDisposedException)
            {
                error = null;
            }
            catch (IOException ex)
            {
                error = ex;
            }
            catch (Exception ex)
            {
                error = new IOException(ex.Message, ex);
            }
            finally
            {
                _ws.Close();
            }

            return error;
        }
        private async Task ProcessSends()
        {
            while (true)
            {
                // Wait for data to write from the pipe producer
                var result = await _application.Input.ReadAsync().ConfigureAwait(false);
                var buffer = result.Buffer;

                if (result.IsCanceled)
                {
                    break;
                }

                var end = buffer.End;
                var isCompleted = result.IsCompleted;
                if (!buffer.IsEmpty)
                {
                    _ws.Send(buffer.ToArray());
                }

                _application.Input.AdvanceTo(end);

                if (isCompleted)
                {
                    break;
                }
            }
        }

        private static ProtocolType DetermineProtocolType(EndPoint endPoint)
        {
            return ProtocolType.Tcp;
        }
    }

}
