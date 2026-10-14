// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copied from https://github.com/dotnet/runtime/blob/60629d14374c56f1cb51819049ad1fa529307f8d/src/libraries/System.Net.ServerSentEvents/src/System/Net/ServerSentEvents/SseItemParser.cs (v10.0.0)

namespace System.Net.ServerSentEvents
{
    /// <summary>Encapsulates a method for parsing the bytes payload of a server-sent event.</summary>
    /// <typeparam name="T">Specifies the type of the return value of the parser.</typeparam>
    /// <param name="eventType">The event's type.</param>
    /// <param name="data">The event's payload bytes.</param>
    /// <returns>The parsed <typeparamref name="T"/>.</returns>
    internal delegate T SseItemParser<out T>(string eventType, ReadOnlySpan<byte> data); // JetBrains fork: public in System.Net.ServerSentEvents
}
