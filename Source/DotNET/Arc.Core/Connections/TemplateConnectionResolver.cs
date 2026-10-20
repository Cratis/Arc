// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.DependencyInjection;

namespace Cratis.Arc.Connections;

/// <summary>
/// Represents an <see cref="IConnectionResolver"/> that resolves from templates with placeholders.
/// </summary>
/// <remarks>
/// Placeholders are written in braces and are matched against the scope of the request without regard to case, for
/// instance <c>Server=sql.example;Database=app-{tenant}</c>. The placeholders <c>{purpose}</c> and <c>{storage}</c>
/// are always available. A placeholder without a value fails rather than producing a connection to the wrong database.
/// </remarks>
/// <param name="options">The <see cref="TemplateConnectionResolverOptions"/>.</param>
[IgnoreConvention]
public partial class TemplateConnectionResolver(TemplateConnectionResolverOptions options) : IConnectionResolver
{
    /// <inheritdoc/>
    public Task<ResolvedConnection> Resolve(ConnectionRequest request, CancellationToken cancellationToken = default)
    {
        var values = new Dictionary<string, string>(request.Scope, StringComparer.OrdinalIgnoreCase)
        {
            [WellKnownConnectionScopeKeys.Purpose] = request.Purpose.ToString(),
            [WellKnownConnectionScopeKeys.Storage] = request.Storage
        };

        return Task.FromResult(new ResolvedConnection(
            Apply(options.ConnectionString, values),
            Apply(options.Database, values),
            Apply(options.Schema, values)));
    }

    static string? Apply(string? template, Dictionary<string, string> values) =>
        string.IsNullOrEmpty(template)
            ? null
            : PlaceholderRegex().Replace(template, match =>
            {
                var name = match.Groups["name"].Value;
                return values.TryGetValue(name, out var value) ? value : throw new UnknownConnectionPlaceholder(name, values.Keys);
            });

    [GeneratedRegex(@"\{(?<name>[A-Za-z0-9_\-]+)\}", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex PlaceholderRegex();
}
