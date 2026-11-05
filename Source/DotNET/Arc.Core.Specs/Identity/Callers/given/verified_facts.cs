// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Identity.Callers.given;

internal static class verified_facts
{
    internal static readonly CallerAccountKey Account = new("https://login.example.test", "8b167ad2-0f89-4c78-9d42-6d4580495d32");
    internal static readonly CallerActor Actor = new("https://agents.example.test", "agent-fixture");
    internal static VerifiedCallerInstallation Installation => new("https://api.example.test", "test");
    internal static CallerResourceCatalog Catalog => new(new Dictionary<string, string> { { "https://api.example.test", "example-api" } });
    internal static TenantId Tenant => new("tenant-fixture");

    internal static VerifiedCredentialFacts Person() => new(CallerPrincipalKind.Person, CallerCredentialKind.PersonalAccessToken, new("credential-fixture"), new(Installation.Resource!, "example-api", "test"))
    {
        AccountKey = Account,
        Person = new("person-fixture"),
        Assurance = new("idp-fixture", 1234, "basic", null),
        Tenant = Tenant,
        Scopes = new HashSet<string> { "fixture:read" }
    };

    internal static VerifiedCredentialFacts Delegated() => Person() with
    {
        PrincipalKind = CallerPrincipalKind.DelegatedActor,
        CredentialKind = CallerCredentialKind.OAuthGrant,
        Actor = Actor,
        Grant = new(new("grant-fixture"), Account, Actor, Installation.Resource, "example-api", Tenant, "test")
    };

    internal static VerifiedCredentialFacts Service() => new(CallerPrincipalKind.OrganizationAutomation, CallerCredentialKind.MachinePrincipal, new("machine-fixture"), new(Installation.Resource!, "example-api", "test"))
    {
        ServicePrincipal = new("service-fixture"),
        Permissions = new HashSet<string> { "fixture:read" }
    };
}
