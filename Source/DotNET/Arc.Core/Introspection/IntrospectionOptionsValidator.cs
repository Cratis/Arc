// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Introspection;

/// <summary>
/// Validates the command and query catalog exposure configuration on host startup.
/// </summary>
public class IntrospectionOptionsValidator : IValidateOptions<ArcOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, ArcOptions options)
    {
        var introspection = options.Introspection;
        if (introspection.Roles is not null &&
            (!introspection.RequireAuthentication || introspection.Roles.Split(',').Any(role => string.IsNullOrWhiteSpace(role))))
        {
            return ValidateOptionsResult.Fail("Cratis:Arc:Introspection:Roles requires RequireAuthentication=true and a comma-separated list of nonempty roles.");
        }

        if (introspection.TrustForwardedIdentityHeaders && (!introspection.Enabled || !introspection.RequireAuthentication))
        {
            return ValidateOptionsResult.Fail("Cratis:Arc:Introspection:TrustForwardedIdentityHeaders requires Enabled=true and RequireAuthentication=true.");
        }

        return ValidateOptionsResult.Success;
    }
}
