// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.Sinks;

/// <summary>
/// Reads the server of a named MongoDB sink and tells what is wrong with its configuration.
/// </summary>
static class MongoDBSinkServers
{
    /// <summary>
    /// Get the server connection string of a sink.
    /// </summary>
    /// <param name="sinkName">The name of the sink.</param>
    /// <param name="sink">The <see cref="MongoDBSinkOptions"/>.</param>
    /// <returns>The connection string.</returns>
    /// <exception cref="InvalidMongoDBSinkConfiguration">Thrown when the sink is not configured correctly.</exception>
    public static string GetServer(string sinkName, MongoDBSinkOptions sink)
    {
        var problem = GetProblem(sinkName, sink, out var server);
        return problem is null ? server! : throw new InvalidMongoDBSinkConfiguration(sinkName, problem);
    }

    /// <summary>
    /// Get what is wrong with the configuration of a sink.
    /// </summary>
    /// <param name="sinkName">The name of the sink.</param>
    /// <param name="sink">The <see cref="MongoDBSinkOptions"/>.</param>
    /// <param name="server">The server connection string when the configuration is valid.</param>
    /// <returns>The problem, or null when the configuration is valid.</returns>
    /// <remarks>A problem names settings and paths, never what a file holds.</remarks>
    public static string? GetProblem(string sinkName, MongoDBSinkOptions? sink, out string? server)
    {
        server = null;
        var section = $"Sinks:{sinkName}";
        if (sink is null)
        {
            return $"'{section}' has no settings; set 'Server' or 'ServerFile'";
        }

        var hasServer = !string.IsNullOrWhiteSpace(sink.Server);
        var hasServerFile = !string.IsNullOrWhiteSpace(sink.ServerFile);
        if (hasServer && hasServerFile)
        {
            return $"'{section}:Server' and '{section}:ServerFile' are both set; set only one of them";
        }

        if (!hasServer && !hasServerFile)
        {
            return $"'{section}' sets neither 'Server' nor 'ServerFile'";
        }

        if (hasServer)
        {
            server = sink.Server!.Trim();
            return null;
        }

        var path = sink.ServerFile!.Trim();
        if (!File.Exists(path))
        {
            return $"'{section}:ServerFile' names '{path}', which does not exist or is not a file";
        }

        try
        {
            server = File.ReadAllText(path).Trim();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return $"'{section}:ServerFile' names '{path}', which cannot be read ({ex.GetType().Name})";
        }

        return string.IsNullOrEmpty(server) ? $"'{section}:ServerFile' names '{path}', which is empty" : null;
    }
}
