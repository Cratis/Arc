// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.PersonalAccessTokens;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc;

/// <summary>Opt-in PAT caller installation, independent of signed browser forwarding.</summary>
public static class PersonalAccessTokensArcBuilderExtensions
{
    /// <summary>
    /// Installs resource-local PATs. Requires a resource, audience, environment and person directory.
    /// Without authoritative storage, recovery and policy adapters, issuance and verification fail closed.
    /// </summary>
    /// <remarks>
    /// To verify signed browser callers for issuance on the same resource, configure signed v2 forwarding and
    /// call <see cref="VerifiedCallersBuilder.AllowPersonalAccessTokenCallers"/> in the same delegate.
    /// This is one installation; do not also call WithVerifiedCallers. Capability endpoints remain PAT-only.
    /// </remarks>
    /// <param name="builder">The Arc builder.</param>
    /// <param name="options">The explicit resource key, scope and lifetime limits and retired namespaces.</param>
    /// <param name="configure">The receiving caller installation, optionally composing signed browser issuance with PAT callers.</param>
    /// <returns>The builder for continued configuration.</returns>
    public static IArcBuilder WithPersonalAccessTokens(this IArcBuilder builder, PatOptions options, Action<VerifiedCallersBuilder> configure)
    {
        // Validate options before touching the service collection. The builder validates its target before registration.
        var validated = PatSettings.Create(options, new(string.Empty, string.Empty, string.Empty));
        var installation = new VerifiedCallersBuilder();
        configure(installation);
        var target = installation.InstallPersonalAccessTokens(builder.Services, validated.ReservedLegacyPrefixes);
        builder.Services.AddOptions<PatAdapterRegistrationOptions>()
            .Configure(options => options.Services = builder.Services)
            .Validate(options => PatAdapterRegistrationCheck.Validate(options.Services) is null, "Ambiguous adapter registration.")
            .ValidateOnStart();
        builder.Services.AddSingleton(validated with { Target = target });
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.TryAddScoped<IPatAuthority, UnavailablePatAuthority>();
        builder.Services.TryAddScoped<IPatRecoveryEpoch, UnavailablePatRecoveryEpoch>();
        builder.Services.TryAddScoped<IPatPolicy, UnavailablePatPolicy>();
        builder.Services.AddScoped<IPatIssuanceContext, VerifiedPatIssuanceContext>();
        builder.Services.AddScoped<IPersonalAccessTokens, PersonalAccessTokens.PersonalAccessTokens>();
        builder.Services.AddScoped<IVerifiedCredentialSource, PatCredentialSource>();
        return builder;
    }
}
