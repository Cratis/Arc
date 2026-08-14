// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Represents an implementation of <see cref="IValidateOptions{TOptions}"/> that refuses a tenancy configuration no
/// request could resolve a tenant from.
/// </summary>
/// <remarks>
/// <para>
/// The tenant resolver is created by a factory the first time something asks for it, and a factory registration is
/// not something the service provider can check when it is built. Without this the misconfiguration would surface on
/// the first request instead - after the process has reported itself started and healthy, with a real user waiting on
/// the failure. Arc registers <see cref="ArcOptions"/> with <c>ValidateOnStart</c>, so this runs while the host is
/// starting and stops it there.
/// </para>
/// <para>
/// It throws <see cref="BaseDomainIsNotADomainName"/> rather than returning a failed
/// <see cref="ValidateOptionsResult"/>, because that is the exception <c>UseSubdomainTenancy</c> and the resolver
/// already throw for the same configuration, and it says what is wrong and how to fix it. A failure result would be
/// collected into a generic options exception and read as one more validation message.
/// </para>
/// </remarks>
public class TenancyOptionsValidator : IValidateOptions<ArcOptions>
{
    /// <summary>
    /// Validates the tenancy configuration.
    /// </summary>
    /// <param name="name">The name of the options instance being validated.</param>
    /// <param name="options">The <see cref="ArcOptions"/> to validate.</param>
    /// <returns>The <see cref="ValidateOptionsResult"/> of the validation.</returns>
    /// <exception cref="BaseDomainIsNotADomainName">
    /// Thrown when subdomain tenancy is configured with a base domain no host could be matched against.
    /// </exception>
    public ValidateOptionsResult Validate(string? name, ArcOptions options)
    {
        if (options.Tenancy.ResolverType == TenantResolverType.Subdomain)
        {
            BaseDomainIsNotADomainName.ThrowIfNotADomainName(options.Tenancy.BaseDomain);
        }

        return ValidateOptionsResult.Success;
    }
}
