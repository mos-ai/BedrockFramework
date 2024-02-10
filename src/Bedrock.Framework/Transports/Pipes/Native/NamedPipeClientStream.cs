using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Native.System.IO.Pipes;

#if NETSTANDARD2_1 || NETSTANDARD2_2 || NETCOREAPP
public partial class NamedPipeClientStream : global::System.IO.Pipes.PipeStream
{
    private readonly global::System.IO.Pipes.NamedPipeClientStream _underlyingClient;

    public NamedPipeClientStream(string pipeName)
        : this(".", pipeName, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.None, HandleInheritability.None)
    {
    }

    public NamedPipeClientStream(string serverName, string pipeName)
        : this(serverName, pipeName, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.None, HandleInheritability.None)
    {
    }

    public NamedPipeClientStream(string serverName, string pipeName, PipeDirection direction)
        : this(serverName, pipeName, direction, PipeOptions.None, TokenImpersonationLevel.None, HandleInheritability.None)
    {
    }

    public NamedPipeClientStream(string serverName, string pipeName, PipeDirection direction, PipeOptions options)
        : this(serverName, pipeName, direction, options, TokenImpersonationLevel.None, HandleInheritability.None)
    {
    }

    public NamedPipeClientStream(string serverName, string pipeName, PipeDirection direction,
        PipeOptions options, TokenImpersonationLevel impersonationLevel)
        : this(serverName, pipeName, direction, options, impersonationLevel, HandleInheritability.None)
    {
    }

    public NamedPipeClientStream(string serverName, string pipeName, PipeDirection direction,
        PipeOptions options, TokenImpersonationLevel impersonationLevel, HandleInheritability inheritability)
        : base(direction, 4096)
    {
        _underlyingClient = new global::System.IO.Pipes.NamedPipeClientStream(serverName, pipeName, direction, options, impersonationLevel, inheritability);
    }

    public void Connect() => _underlyingClient.Connect();

    public Task ConnectAsync() => _underlyingClient.ConnectAsync();

    public Task ConnectAsync(int timeout) => _underlyingClient.ConnectAsync(timeout);

    public Task ConnectAsync(CancellationToken cancellationToken) => _underlyingClient.ConnectAsync(cancellationToken);

    public Task ConnectAsync(int timeout, CancellationToken cancellationToken) => _underlyingClient.ConnectAsync(timeout, cancellationToken);

    /// <summary>
    /// Closes the named pipe already opened.
    /// </summary>
    public void Disconnect() => _underlyingClient.Close();

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => _underlyingClient.BeginRead(buffer, offset, count, callback, state);
    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => _underlyingClient.BeginWrite(buffer, offset, count, callback, state);
    public override int EndRead(IAsyncResult asyncResult) => _underlyingClient.EndRead(asyncResult);
    public override void EndWrite(IAsyncResult asyncResult) => _underlyingClient.EndWrite(asyncResult);
    public override void Flush() => _underlyingClient.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => _underlyingClient.FlushAsync(cancellationToken);
    public override int Read(byte[] buffer, int offset, int count) => _underlyingClient.Read(buffer, offset, count);
    public override int Read(Span<byte> buffer) => _underlyingClient.Read(buffer);
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _underlyingClient.ReadAsync(buffer, offset, count, cancellationToken);
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => _underlyingClient.ReadAsync(buffer, cancellationToken);
    public override int ReadByte() => _underlyingClient.ReadByte();
    public override long Seek(long offset, SeekOrigin origin) => _underlyingClient.Seek(offset, origin);
    public override void SetLength(long value) => _underlyingClient.SetLength(value);
    public new void WaitForPipeDrain() => _underlyingClient.WaitForPipeDrain();
    public override void Write(ReadOnlySpan<byte> buffer) => _underlyingClient.Write(buffer);
    public override void Write(byte[] buffer, int offset, int count) => _underlyingClient.Write(buffer, offset, count);
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _underlyingClient.WriteAsync(buffer, offset, count, cancellationToken);
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => _underlyingClient.WriteAsync(buffer, cancellationToken);
    public override void WriteByte(byte value) => _underlyingClient.WriteByte(value);
    public override ValueTask DisposeAsync() => _underlyingClient.DisposeAsync();
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _underlyingClient.Dispose();
        }
    }
}
#elif !UseNativeNamedPipes
using Microsoft.Win32.SafeHandles;

/// <summary>
/// Named pipe client. Use this to open the client end of a named pipes created with
/// NamedPipeServerStream.
/// </summary>
public sealed partial class NamedPipeClientStream : global::System.IO.Pipes.PipeStream
{
    // Maximum interval in milliseconds between which cancellation is checked.
    // Used by ConnectInternal. 50ms is fairly responsive time but really long time for processor.
    private const int CancellationCheckInterval = 50;
    private readonly string? _normalizedPipePath;
    private readonly TokenImpersonationLevel _impersonationLevel;
    private readonly PipeOptions _pipeOptions;
    private readonly HandleInheritability _inheritability;
    private readonly PipeDirection _direction;

    // Creates a named pipe client using default server (same machine, or "."), and PipeDirection.InOut
    public NamedPipeClientStream(string pipeName)
        : this(".", pipeName, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.None, HandleInheritability.None)
    {
    }

    public NamedPipeClientStream(string serverName, string pipeName)
        : this(serverName, pipeName, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.None, HandleInheritability.None)
    {
    }

    public NamedPipeClientStream(string serverName, string pipeName, PipeDirection direction)
        : this(serverName, pipeName, direction, PipeOptions.None, TokenImpersonationLevel.None, HandleInheritability.None)
    {
    }

    public NamedPipeClientStream(string serverName, string pipeName, PipeDirection direction, PipeOptions options)
        : this(serverName, pipeName, direction, options, TokenImpersonationLevel.None, HandleInheritability.None)
    {
    }

    public NamedPipeClientStream(string serverName, string pipeName, PipeDirection direction,
        PipeOptions options, TokenImpersonationLevel impersonationLevel)
        : this(serverName, pipeName, direction, options, impersonationLevel, HandleInheritability.None)
    {
    }

    public NamedPipeClientStream(string serverName, string pipeName, PipeDirection direction,
        PipeOptions options, TokenImpersonationLevel impersonationLevel, HandleInheritability inheritability)
        : base(direction, 4096)
    {
        ArgumentThrowHelper.ThrowIfNullOrEmpty(pipeName);
        ArgumentNullThrowHelper.ThrowIfNull(serverName);
        if (serverName.Length == 0)
        {
            throw new ArgumentException("Argument_EmptyServerName");
        }
        if ((options & ~(PipeOptions.WriteThrough | PipeOptions.Asynchronous)) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "ArgumentOutOfRange_OptionsInvalid");
        }
        if (impersonationLevel < TokenImpersonationLevel.None || impersonationLevel > TokenImpersonationLevel.Delegation)
        {
            throw new ArgumentOutOfRangeException(nameof(impersonationLevel), "ArgumentOutOfRange_ImpersonationInvalid");
        }
        if (inheritability < HandleInheritability.None || inheritability > HandleInheritability.Inheritable)
        {
            throw new ArgumentOutOfRangeException(nameof(inheritability), "ArgumentOutOfRange_HandleInheritabilityNoneOrInheritable");
        }

        _normalizedPipePath = GetPipePath(serverName, pipeName);
        _direction = direction;
        _inheritability = inheritability;
        _impersonationLevel = impersonationLevel;
        _pipeOptions = options;
    }

    // Create a NamedPipeClientStream from an existing server pipe handle.
    public NamedPipeClientStream(PipeDirection direction, bool isAsync, bool isConnected, SafePipeHandle safePipeHandle)
        : base(direction, 4096)
    {
        ArgumentNullThrowHelper.ThrowIfNull(safePipeHandle);

        if (safePipeHandle.IsInvalid)
        {
            throw new ArgumentException("Argument_InvalidHandle", nameof(safePipeHandle));
        }
        PipeStream.ValidateHandleIsPipe(safePipeHandle);

        InitializeHandle(safePipeHandle, true, isAsync);
        if (isConnected)
        {
            State = PipeState.Connected;
        }
    }

    ~NamedPipeClientStream()
    {
        Dispose(false);
    }

    public void Connect()
    {
        Connect(Timeout.Infinite);
    }

    public void Connect(int timeout)
    {
        CheckConnectOperationsClient();

        ArgumentOutOfRangeThrowHelper.ThrowIfLessThan(timeout, Timeout.Infinite);

        ConnectInternal(timeout, CancellationToken.None, Environment.TickCount);
    }

    public void Connect(TimeSpan timeout) => Connect(ToTimeoutMilliseconds(timeout));

    private static string GetPipePath(string serverName, string pipeName)
    {
        string normalizedPipePath = Path.GetFullPath(@"\\" + serverName + @"\pipe\" + pipeName);
        if (String.Equals(normalizedPipePath, @"\\.\pipe\anonymous", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentOutOfRangeException("pipeName", "ArgumentOutOfRange_AnonymousReserved");
        }
        return normalizedPipePath;
    }

    private void ConnectInternal(int timeout, CancellationToken cancellationToken, int startTime)
    {
        // This is the main connection loop. It will loop until the timeout expires.
        int elapsed = 0;
        SpinWait sw = default;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Determine how long we should wait in this connection attempt
            int waitTime = timeout == Timeout.Infinite ? CancellationCheckInterval : timeout - elapsed;
            if (cancellationToken.CanBeCanceled && waitTime > CancellationCheckInterval)
            {
                waitTime = CancellationCheckInterval;
            }

            // Try to connect.
            if (TryConnect(waitTime))
            {
                return;
            }

            // Some platforms may return immediately from TryConnect if the connection could not be made,
            // e.g. WaitNamedPipe on Win32 will return immediately if the pipe hasn't yet been created,
            // and open on Unix will fail if the file isn't yet available.  Rather than just immediately
            // looping around again, do slightly smarter busy waiting.
            sw.SpinOnce();
        }
        while (timeout == Timeout.Infinite || (elapsed = unchecked(Environment.TickCount - startTime)) < timeout);

        throw new TimeoutException();
    }

    public Task ConnectAsync()
    {
        // We cannot avoid creating lambda here by using Connect method
        // unless we don't care about start time to be measured before the thread is started
        return ConnectAsync(Timeout.Infinite, CancellationToken.None);
    }

    public Task ConnectAsync(int timeout)
    {
        return ConnectAsync(timeout, CancellationToken.None);
    }

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        return ConnectAsync(Timeout.Infinite, cancellationToken);
    }

    public Task ConnectAsync(int timeout, CancellationToken cancellationToken)
    {
        CheckConnectOperationsClient();

        ArgumentOutOfRangeThrowHelper.ThrowIfLessThan(timeout, Timeout.Infinite);

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        int startTime = Environment.TickCount; // We need to measure time here, not in the lambda

        return Task.Factory.StartNew(static state =>
        {
            var tuple = ((NamedPipeClientStream stream, int timeout, CancellationToken cancellationToken, int startTime))state!;
            tuple.stream.ConnectInternal(tuple.timeout, tuple.cancellationToken, tuple.startTime);
        }, (this, timeout, cancellationToken, startTime), cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    public Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken = default) =>
        ConnectAsync(ToTimeoutMilliseconds(timeout), cancellationToken);

    private static int ToTimeoutMilliseconds(TimeSpan timeout)
    {
        long totalMilliseconds = (long)timeout.TotalMilliseconds;
        ArgumentOutOfRangeThrowHelper.ThrowIfLessThan(totalMilliseconds, -1, nameof(timeout));
        ArgumentOutOfRangeThrowHelper.ThrowIfGreaterThan(totalMilliseconds, int.MaxValue, nameof(timeout));
        return (int)totalMilliseconds;
    }

    // override because named pipe clients can't get/set properties when waiting to connect
    // or broken
    protected override void CheckPipePropertyOperations()
    {
        base.CheckPipePropertyOperations();

        if (State == PipeState.WaitingToConnect)
        {
            throw new InvalidOperationException("InvalidOperation_PipeNotYetConnected");
        }
        if (State == PipeState.Broken)
        {
            throw new IOException("IO_PipeBroken");
        }
    }

    // named client is allowed to connect from broken
    private void CheckConnectOperationsClient()
    {
        if (State == PipeState.Connected)
        {
            throw new InvalidOperationException("InvalidOperation_PipeAlreadyConnected");
        }
        if (State == PipeState.Closed)
        {
            throw new ObjectDisposedException(null, "ObjectDisposed_PipeClosed");
        }
    }
}
#endif
