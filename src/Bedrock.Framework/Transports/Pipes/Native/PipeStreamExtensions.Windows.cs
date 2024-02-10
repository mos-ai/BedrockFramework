// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// https://github.com/dotnet/runtime/blob/241d558c65e68bbe770f4b61f1711cd889c8e498/src/libraries/System.IO.Pipes/src/System/IO/Pipes/PipeStream.Windows.cs

using Microsoft.Win32.SafeHandles;

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

#if NETSTANDARD2_0 && !UseNativeNamedPipes
namespace Native.System.IO.Pipes;
public abstract partial class PipeStream
{
    // ************************ Static Methods ************************ //

    [SecurityCritical]
    internal static Interop.SECURITY_ATTRIBUTES GetSecAttrs(HandleInheritability inheritability)
    {
        Interop.SECURITY_ATTRIBUTES secAttrs = default(Interop.SECURITY_ATTRIBUTES);
        if ((inheritability & HandleInheritability.Inheritable) != 0)
        {
            secAttrs = new Interop.SECURITY_ATTRIBUTES();
            secAttrs.nLength = (uint)Marshal.SizeOf(secAttrs);
            secAttrs.bInheritHandle = true;
        }
        return secAttrs;
    }

    // When doing IO asynchronously (i.e., m_isAsync==true), this callback is 
    // called by a free thread in the threadpool when the IO operation 
    // completes.  
    [SecurityCritical]
    unsafe private static void AsyncPSCallback(uint errorCode, uint numBytes, NativeOverlapped* pOverlapped)
    {
        // Unpack overlapped
        Overlapped overlapped = Overlapped.Unpack(pOverlapped);
        // Free the overlapped struct in EndRead/EndWrite.

        // Extract async result from overlapped 
        PipeStreamAsyncResult asyncResult = (PipeStreamAsyncResult)overlapped.AsyncResult;
        asyncResult._numBytes = (int)numBytes;

        // Allow async read to finish
        if (!asyncResult._isWrite)
        {
            if (errorCode == Interop.ERROR_BROKEN_PIPE ||
                errorCode == Interop.ERROR_PIPE_NOT_CONNECTED ||
                errorCode == Interop.ERROR_NO_DATA)
            {
                errorCode = 0;
                numBytes = 0;
            }
        }

        // For message type buffer.
        if (errorCode == Interop.ERROR_MORE_DATA)
        {
            errorCode = 0;
            asyncResult._isMessageComplete = false;
        }
        else
        {
            asyncResult._isMessageComplete = true;
        }

        asyncResult._errorCode = (int)errorCode;

        // Call the user-provided callback.  It can and often should
        // call EndRead or EndWrite.  There's no reason to use an async 
        // delegate here - we're already on a threadpool thread.  
        // IAsyncResult's completedSynchronously property must return
        // false here, saying the user callback was called on another thread.
        asyncResult._completedSynchronously = false;
        asyncResult._isComplete = true;

        // The OS does not signal this event.  We must do it ourselves.
        ManualResetEvent wh = asyncResult._waitHandle;
        if (wh != null)
        {
            Debug.Assert(!wh.GetSafeWaitHandle().IsClosed, "ManualResetEvent already closed!");
            bool r = wh.Set();
            Debug.Assert(r, "ManualResetEvent::Set failed!");
            if (!r)
            {
                throw new global::System.Exception($"Internal Error: {Marshal.GetLastWin32Error()}");
            }
        }

        AsyncCallback callback = asyncResult._userCallback;
        if (callback != null)
        {
            callback(asyncResult);
        }
    }

    /// <summary>Throws an exception if the supplied handle does not represent a valid pipe.</summary>
    /// <param name="safePipeHandle">The handle to validate.</param>
    internal static void ValidateHandleIsPipe(SafePipeHandle safePipeHandle)
    {
        // Check that this handle is infact a handle to a pipe.
        if (Interop.mincore.GetFileType(safePipeHandle) != Interop.FILE_TYPE_PIPE)
        {
            throw new IOException("IO_InvalidPipeHandle");
        }
    }
}
#endif