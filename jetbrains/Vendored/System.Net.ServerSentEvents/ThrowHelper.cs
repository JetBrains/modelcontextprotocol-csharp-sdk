// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copied from https://github.com/dotnet/runtime/blob/60629d14374c56f1cb51819049ad1fa529307f8d/src/libraries/System.Net.ServerSentEvents/src/System/Net/ServerSentEvents/ThrowHelper.cs (v10.0.0)

using System.Diagnostics.CodeAnalysis;

namespace System.Net.ServerSentEvents
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        public static void ThrowArgumentNullException(string parameterName)
        {
            throw new ArgumentNullException(parameterName);
        }

        public static void ThrowArgumentException_CannotContainLineBreaks(string parameterName)
        {
            throw new ArgumentException(SR.ArgumentException_CannotContainLineBreaks, parameterName);
        }

        public static void ThrowArgumentException_CannotBeNegative(string parameterName)
        {
            throw new ArgumentException(SR.ArgumentException_CannotBeNegative, parameterName);
        }
    }
}
