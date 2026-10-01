// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Introspection;

/// <summary>
/// Validates the discovery endpoint exposure configuration on host startup.
/// </summary>
public class IntrospectionOptionsValidator : IValidateOptions<ArcOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, ArcOptions options)
    {
        return ValidateOptions(options.Introspection);
    }

    /// <summary>
    /// Validates discovery exposure settings for both configured hosts and direct mapper calls.
    /// </summary>
    /// <param name="introspection">The discovery exposure settings.</param>
    /// <returns>The validation result.</returns>
    internal static ValidateOptionsResult ValidateOptions(IntrospectionOptions introspection)
    {
        if (introspection.Roles is not null)
        {
            var roles = introspection.Roles.Split(',').Select(role => role.Trim()).ToArray();
            if (introspection.RequireAuthentication == false || roles.Any(string.IsNullOrWhiteSpace))
            {
                return ValidateOptionsResult.Fail("Cratis:Arc:Introspection:Roles cannot be combined with RequireAuthentication=false and must be a comma-separated list of nonempty roles.");
            }
            introspection.Roles = string.Join(',', roles);
        }

        return ValidateOptionsResult.Success;
    }
}
