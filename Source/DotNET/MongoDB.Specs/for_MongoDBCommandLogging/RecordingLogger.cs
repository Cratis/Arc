// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.MongoDB.for_MongoDBCommandLogging;

public class RecordingLogger(LogLevel minimumLevel) : ILogger<MongoDBClientFactory>
{
    public List<(LogLevel Level, string Message, IReadOnlyDictionary<string, object?> State)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= minimumLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var values = (state as IEnumerable<KeyValuePair<string, object?>>)?.ToDictionary(_ => _.Key, _ => _.Value) ?? [];
        Entries.Add((logLevel, formatter(state, exception), values));
    }
}
