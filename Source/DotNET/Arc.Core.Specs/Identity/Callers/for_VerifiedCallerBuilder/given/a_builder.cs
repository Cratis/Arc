// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers.given;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerBuilder.given;

public class a_builder : Specification
{
    internal VerifiedCredentialFacts _facts;
    internal VerifiedCallerInstallation _installation;
    internal CallerResourceCatalog _catalog;
    internal TenantId? _tenant;
    internal CallerConstruction _result;

    void Establish()
    {
        _facts = verified_facts.Person();
        _installation = verified_facts.Installation;
        _catalog = verified_facts.Catalog;
        _tenant = verified_facts.Tenant;
    }

    internal void Build() => _result = new VerifiedCallerBuilder(_catalog).Build(_facts, _installation, _tenant);
}
