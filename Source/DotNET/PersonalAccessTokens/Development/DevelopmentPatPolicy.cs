// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.PersonalAccessTokens.Development;

/// <summary>
/// Development-only policy bounded by the installed PAT resource, scopes and lifetime.
/// It does not model real membership, SSO freshness or organization policy; never use it in production.
/// Register with <see cref="DevelopmentPatArcBuilderExtensions.UseDevelopmentPersonalAccessTokenAdapters"/>.
/// </summary>
public sealed class DevelopmentPatPolicy : IPatPolicy
{
    readonly IHostEnvironment _environment;
    readonly PatSettings _settings;
    readonly TimeProvider _clock;

    internal DevelopmentPatPolicy(IHostEnvironment environment, PatSettings settings, TimeProvider clock)
    {
        _environment = environment;
        _settings = settings;
        _clock = clock;
    }

    /// <inheritdoc/>
    public Task<PatPolicyDecision> Evaluate(PatRecord record, bool issuing, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new PatPolicyDecision(Permits(record), _settings.MaximumLifetime, 0));
    }

    bool Permits(PatRecord record) =>
        _environment.IsDevelopment() && !record.Revoked && MatchesInstallation(record) &&
        WithinLifetime(record) && _settings.Allows(record.Scopes);

    bool MatchesInstallation(PatRecord record) =>
        record.ResourceKey == _settings.ResourceKey.Value && record.Resource == _settings.Target.Resource &&
        record.Audience == _settings.Target.Audience && record.Environment == _settings.Target.Environment;

    bool WithinLifetime(PatRecord record) =>
        record.CreatedAt <= _clock.GetUtcNow() && record.ExpiresAt > _clock.GetUtcNow() &&
        record.ExpiresAt > record.CreatedAt && record.ExpiresAt - record.CreatedAt <= _settings.MaximumLifetime;
}
