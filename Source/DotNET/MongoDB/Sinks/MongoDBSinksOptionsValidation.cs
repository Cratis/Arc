// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.Sinks;

/// <summary>
/// Validates the named sinks of <see cref="MongoDBOptions"/>, so a misconfigured sink stops startup with a message naming it.
/// </summary>
[IgnoreConvention]
public class MongoDBSinksOptionsValidation : IValidateOptions<MongoDBOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, MongoDBOptions options)
    {
        var problems = options.Sinks
            .Select(_ => MongoDBSinkServers.GetProblem(_.Key, _.Value, out var server) ?? GetServerProblem(_.Key, server!))
            .OfType<string>()
            .Select(_ => $"MongoDB {_}.")
            .ToArray();

        return problems.Length == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(problems);
    }

    static string? GetServerProblem(string sinkName, string server)
    {
        try
        {
            _ = new MongoUrl(server);
            return null;
        }
        catch (Exception ex) when (ex is FormatException or MongoConfigurationException or ArgumentException)
        {
            return $"sink '{sinkName}' has a server that is not a MongoDB connection string";
        }
    }
}
