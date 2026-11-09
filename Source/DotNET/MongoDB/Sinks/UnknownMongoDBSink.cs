// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.Sinks;

/// <summary>
/// The exception that is thrown when a read model is stored in a named sink that has no MongoDB configuration.
/// </summary>
/// <param name="sinkName">The name of the sink.</param>
/// <param name="configured">The names of the configured sinks.</param>
public class UnknownMongoDBSink(string sinkName, IEnumerable<string> configured)
    : Exception($"No MongoDB sink named '{sinkName}' is configured. Configure it under 'Cratis:MongoDB:Sinks:{sinkName}' with 'Server' or 'ServerFile'. Configured sinks: {Describe(configured)}.")
{
    static string Describe(IEnumerable<string> configured)
    {
        var names = configured.Select(_ => $"'{_}'").ToArray();
        return names.Length == 0 ? "none" : string.Join(", ", names);
    }
}
