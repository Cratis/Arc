// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers.given;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerBuilder.when_building;

public class with_invalid_bindings : given.a_builder
{
    readonly Dictionary<CallerRefusal, CallerConstruction> _refusals = [];

    void Because()
    {
        Refuse(CallerRefusal.UnsupportedProfile, _facts with { ProfileVersion = "2" });
        Refuse(CallerRefusal.ResourceMismatch, _facts with { Resource = "https://other.example.test" });
        Refuse(CallerRefusal.EnvironmentMismatch, _facts with { Environment = "production" });
        Refuse(CallerRefusal.AudienceMismatch, _facts with { Audience = "other-api" });
        Refuse(CallerRefusal.TenantMismatch, _facts with { Tenant = new("other-tenant") });
        Refuse(CallerRefusal.InvalidAccountKey, _facts with { AccountKey = _facts.AccountKey with { Subject = "8B167AD2-0F89-4C78-9D42-6D4580495D32" } });
        Refuse(CallerRefusal.MissingRequiredField, _facts with { Assurance = null });
        Refuse(CallerRefusal.ServiceCarriesPersonFields, verified_facts.Service() with { Person = new("injected-person"), Tenant = _tenant });
        Refuse(CallerRefusal.InstallationNotConfigured, _facts, new(null, null));
    }

    [Fact] void should_refuse_each_invalid_binding() => _refusals.All(pair => pair.Value.Refusal == pair.Key).ShouldBeTrue();
    [Fact] void should_never_construct_a_caller() => _refusals.Values.All(result => result.Caller is null).ShouldBeTrue();

    void Refuse(CallerRefusal reason, VerifiedCredentialFacts facts, VerifiedCallerInstallation? installation = null) =>
        _refusals.Add(reason, new VerifiedCallerBuilder(_catalog).Build(facts, installation ?? _installation, _tenant));
}
