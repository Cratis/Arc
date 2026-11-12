// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.PersonalAccessTokens.for_DevelopmentPatAdapters.given;

namespace Cratis.Arc.PersonalAccessTokens.for_DevelopmentPatAdapters;

public class when_policy_bounds_are_exceeded : a_development_pat_installation
{
    PatPolicyDecision[] _decisions;

    async Task Because()
    {
        var record = _facts.Record;
        var invalid = new[]
        {
            record with { Scopes = new HashSet<string> { "unconfigured" } },
            record with { Resource = "other-resource" },
            record with { ResourceKey = "other-key" },
            record with { Audience = "other-audience" },
            record with { Environment = "other-environment" },
            record with { ExpiresAt = record.CreatedAt + _facts.Settings.MaximumLifetime + TimeSpan.FromMinutes(1) },
            record with { ExpiresAt = record.CreatedAt },
            record with { Revoked = true }
        };
        _decisions = await Task.WhenAll(invalid.Select(_ => _policy.Evaluate(_, true, TestContext.Current.CancellationToken)));
    }

    [Fact] void should_deny_every_out_of_bounds_record() => _decisions.Select(_ => _.Permitted).Distinct().ShouldContainOnly(false);
}
