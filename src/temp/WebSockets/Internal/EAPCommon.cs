//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

// https://github.com/dotnet/samples/blob/main/csharp/parallel/ParallelExtensionsExtras/Extensions/EAP/EAPCommon.cs

using System.ComponentModel;

namespace System.Threading.Tasks
{
    internal class EAPCommon
    {
        internal static void HandleCompletion<T>(
            TaskCompletionSource<T> tcs, AsyncCompletedEventArgs e, Func<T> getResult, Action unregisterHandler)
        {
            // Transfers the results from the AsyncCompletedEventArgs and getResult() to the
            // TaskCompletionSource, but only AsyncCompletedEventArg's UserState matches the TCS
            // (this check is important if the same WebClient is used for multiple, asynchronous
            // operations concurrently).  Also unregisters the handler to avoid a leak.
            if (e.UserState == tcs)
            {
                if (e.Cancelled) tcs.TrySetCanceled();
                else if (e.Error != null) tcs.TrySetException(e.Error);
                else tcs.TrySetResult(getResult());
                unregisterHandler();
            }
        }

        internal static void HandleCompletionSuccess<T>(
            TaskCompletionSource<T> tcs, Func<T> getResult, Action unregisterHandler)
        {
            var asyncEventArgs = new AsyncCompletedEventArgs(null, false, tcs);
            HandleCompletion(tcs, asyncEventArgs, getResult, unregisterHandler);
        }

        internal static void HandleCompletionCancelled<T>(
            TaskCompletionSource<T> tcs, Func<T> getResult, Action unregisterHandler)
        {
            var asyncEventArgs = new AsyncCompletedEventArgs(null, true, tcs);
            HandleCompletion(tcs, asyncEventArgs, getResult, unregisterHandler);
        }

        internal static void HandleCompletionException<T>(
            TaskCompletionSource<T> tcs, Exception e, Action unregisterHandler)
        {
            var asyncEventArgs = new AsyncCompletedEventArgs(e, false, tcs);
            HandleCompletion(tcs, asyncEventArgs, null!, unregisterHandler);
        }
    }
}