#if (NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// https://raw.githubusercontent.com/dotnet/runtime/main/src/libraries/System.Memory/src/System/Buffers/BuffersExtensions.cs

using System.Runtime.CompilerServices;

namespace System.Buffers;

/// <summary>
/// Extension methods for <see cref="ReadOnlySequence{T}"/>
/// </summary>
public static class BuffersExtensions
{
    /// <summary>
    /// Returns position of first occurrence of item in the <see cref="ReadOnlySequence{T}"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SequencePosition? PositionOf<T>(in this ReadOnlySequence<T> source, ReadOnlySpan<T> value) where T : IEquatable<T>?
    {
        if (source.IsSingleSegment)
        {
            int index = source.First.Span.IndexOf(value);
            if (index != -1)
            {
                return source.GetPosition(index);
            }

            return null;
        }
        else
        {
            return PositionOfMultiSegment(source, value);
        }
    }

    private static SequencePosition? PositionOfMultiSegment<T>(in ReadOnlySequence<T> source, ReadOnlySpan<T> value) where T : IEquatable<T>?
    {
        SequencePosition position = source.Start;
        SequencePosition result = position;
        while (source.TryGet(ref position, out ReadOnlyMemory<T> memory))
        {
            int index = memory.Span.IndexOf(value);
            if (index != -1)
            {
                return source.GetPosition(index, result);
            }
            else if (position.GetObject() == null)
            {
                break;
            }

            result = position;
        }

        return null;
    }
}

#endif
