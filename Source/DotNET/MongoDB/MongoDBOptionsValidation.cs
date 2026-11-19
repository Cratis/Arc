// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Validates that <see cref="MongoDBOptions.Server"/> and <see cref="MongoDBOptions.Database"/> are configured, so a missing
/// connection stops startup with a message naming the configuration key and environment variable to set.
/// </summary>
/// <remarks>
/// There is deliberately no default for either value: connecting to a server nobody chose must never happen silently.
/// </remarks>
/// <param name="sectionPath">The configuration section path <see cref="MongoDBOptions"/> is bound from.</param>
[IgnoreConvention]
public class MongoDBOptionsValidation(string sectionPath) : IValidateOptions<MongoDBOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, MongoDBOptions options)
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(options.Server))
        {
            problems.Add(NotConfigured(nameof(MongoDBOptions.Server), "mongodb://localhost:27017"));
        }

        if (string.IsNullOrWhiteSpace(options.Database))
        {
            problems.Add(NotConfigured(nameof(MongoDBOptions.Database), "my-database"));
        }

        return problems.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(problems);
    }

    string NotConfigured(string setting, string example) =>
        $"MongoDB {setting} is not configured. Set {sectionPath}:{setting} (environment variable {sectionPath.Replace(":", "__")}__{setting}), for example {example}.";
}
