// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// https://github.com/dotnet/runtime/blob/main/src/libraries/System.IO.Pipes/src/System/IO/Pipes/NamedPipeClientStream.Windows.cs

using Microsoft.Win32.SafeHandles;

using System;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;

namespace Native.System.IO.Pipes;

#if NETSTANDARD2_0 && !UseNativeNamedPipes
/// <summary>
/// Named pipe client. Use this to open the client end of a named pipes created with
/// NamedPipeServerStream.
/// </summary>
public sealed partial class NamedPipeClientStream : global::System.IO.Pipes.PipeStream
{
    private PipeState _state;

    // Waits for a pipe instance to become available. This method may return before WaitForConnection is called
    // on the server end, but WaitForConnection will not return until we have returned.  Any data written to the
    // pipe by us after we have connected but before the server has called WaitForConnection will be available
    // to the server after it calls WaitForConnection.
    private bool TryConnect(int timeout)
    {
        Interop.SECURITY_ATTRIBUTES secAttrs = PipeStream.GetSecAttrs(_inheritability);
        
        int _pipeFlags = (int)_pipeOptions;
        if (_impersonationLevel != TokenImpersonationLevel.None)
        {
            _pipeFlags |= Interop.SECURITY_SQOS_PRESENT;
            _pipeFlags |= (((int)_impersonationLevel - 1) << 16);
        }

        int access = 0;
        if ((PipeDirection.In & _direction) != 0)
        {
            access |= Interop.GENERIC_READ;
        }
        if ((PipeDirection.Out & _direction) != 0)
        {
            access |= Interop.GENERIC_WRITE;
        }

        SafePipeHandle handle = CreateNamedPipeClient(_normalizedPipePath, ref secAttrs, _pipeFlags, access);

        if (handle.IsInvalid)
        {
            int errorCode = Marshal.GetLastWin32Error();

            handle.Dispose();

            // CreateFileW: "If the CreateNamedPipe function was not successfully called on the server prior to this operation,
            // a pipe will not exist and CreateFile will fail with ERROR_FILE_NOT_FOUND"
            // WaitNamedPipeW: "If no instances of the specified named pipe exist,
            // the WaitNamedPipe function returns immediately, regardless of the time-out value."
            // We know that no instances exist, so we just quit without calling WaitNamedPipeW.
            if (errorCode == Interop.ERROR_FILE_NOT_FOUND)
            {
                return false;
            }

            if (errorCode != Interop.ERROR_PIPE_BUSY)
            {
                throw new IOException("Error Pipe is Busy.", errorCode);
            }
            
            if (!Interop.mincore.WaitNamedPipe(_normalizedPipePath, timeout))
            {
                errorCode = Marshal.GetLastWin32Error();

                if (errorCode == Interop.ERROR_FILE_NOT_FOUND)// || // server has been closed
                    //errorCode == Interop.ERROR_SEM_TIMEOUT)
                {
                    return false;
                }
                
                throw new IOException("Error: WaitNamedPipe Failed.", errorCode);
            }

            // Pipe server should be free. Let's try to connect to it.
            handle = CreateNamedPipeClient(_normalizedPipePath, ref secAttrs, _pipeFlags, access);

            if (handle.IsInvalid)
            {
                errorCode = Marshal.GetLastWin32Error();

                handle.Dispose();

                // WaitNamedPipe: "A subsequent CreateFile call to the pipe can fail,
                // because the instance was closed by the server or opened by another client."
                if (errorCode == Interop.ERROR_PIPE_BUSY || // opened by another client
                    errorCode == Interop.ERROR_FILE_NOT_FOUND) // server has been closed
                {
                    return false;
                }

                throw new IOException("Error: CreateNamedPipeClient Failed.", errorCode);
            }
        }

        // Success!
        InitializeHandle(handle, false, (_pipeOptions & PipeOptions.Asynchronous) != 0);
        State = PipeState.Connected;
        //ValidateRemotePipeUser();
        return true;

        static SafePipeHandle CreateNamedPipeClient(string? path, ref Interop.SECURITY_ATTRIBUTES secAttrs, int pipeFlags, int access)
            => Interop.mincore.CreateNamedPipeClient(path, access, FileShare.None, ref secAttrs, FileMode.Open, pipeFlags, hTemplateFile: IntPtr.Zero);
    }

    [SupportedOSPlatform("windows")]
    public unsafe int NumberOfServerInstances
    {
        get
        {
            CheckPipePropertyOperations();

            // NOTE: MSDN says that GetNamedPipeHandleState requires that the pipe handle has 
            // GENERIC_READ access, but we don't check for that because sometimes it works without
            // GERERIC_READ access. [Edit: Seems like CreateFile slaps on a READ_ATTRIBUTES 
            // access request before calling NTCreateFile, so all NamedPipeClientStreams can read
            // this if they are created (on WinXP SP2 at least)] 
            int numInstances;
            if (!Interop.mincore.GetNamedPipeHandleState(InternalHandle, Interop.NULL, out numInstances,
                Interop.NULL, Interop.NULL, Interop.NULL, 0))
            {
                throw new IOException("WinIOError", Marshal.GetLastWin32Error());
            }

            return (int)numInstances;
        }
    }

    //private void ValidateRemotePipeUser()
    //{
    //    if (!IsCurrentUserOnly)
    //        return;

    //    PipeSecurity accessControl = this.GetAccessControl();
    //    IdentityReference? remoteOwnerSid = accessControl.GetOwner(typeof(SecurityIdentifier));
    //    using (WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent())
    //    {
    //        SecurityIdentifier? currentUserSid = currentIdentity.Owner;
    //        if (remoteOwnerSid != currentUserSid)
    //        {
    //            State = PipeState.Closed;
    //            throw new UnauthorizedAccessException("Unauthorized Access, Not owned by CurrentUser.");
    //        }
    //    }
    //}

    internal PipeState State
    {
        get
        {
            return _state;
        }
        set
        {
            _state = value;
        }
    }

    private FieldInfo _internalHandleField;

    internal SafePipeHandle? InternalHandle
    {
        get
        {
            _internalHandleField ??= typeof(PipeStream).GetField("_internalHandle", BindingFlags.NonPublic | BindingFlags.Instance);
            return (SafePipeHandle?)_internalHandleField.GetValue(this);
        }
    }
}
#endif