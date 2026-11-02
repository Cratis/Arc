// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries;

/// <summary>
/// Validates the query configuration on host startup.
/// </summary>
public class QueryOptionsValidator : IValidateOptions<ArcOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, ArcOptions options)
    {
        var invalid = options.Query.AllowedWebSocketOrigins
            .Where(entry => entry != WebSocketOrigins.Wildcard && !WebSocketOrigins.TryNormalize(entry, out _))
            .ToArray();

        return invalid.Length == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail($"Cratis:Arc:Query:AllowedWebSocketOrigins entries must be '*' or an origin such as 'https://app.example.com'. Invalid: {string.Join(", ", invalid.Select(_ => $"'{_}'"))}.");
    }
}
