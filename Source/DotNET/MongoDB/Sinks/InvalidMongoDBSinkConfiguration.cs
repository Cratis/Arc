// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.Sinks;

/// <summary>
/// The exception that is thrown when a named MongoDB sink is not configured correctly.
/// </summary>
/// <param name="sinkName">The name of the sink.</param>
/// <param name="problem">What is wrong with its configuration.</param>
public class InvalidMongoDBSinkConfiguration(string sinkName, string problem)
    : Exception($"The MongoDB sink '{sinkName}' is not configured correctly: {problem}.");
