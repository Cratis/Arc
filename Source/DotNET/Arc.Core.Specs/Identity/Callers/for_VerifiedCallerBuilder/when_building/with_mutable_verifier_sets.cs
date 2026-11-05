// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerBuilder.when_building;

public class with_mutable_verifier_sets : given.a_builder
{
    readonly HashSet<string> _scopes = ["fixture:read"];
    readonly HashSet<string> _permissions = ["fixture:execute"];

    void Establish() => _facts = _facts with { Scopes = _scopes, Permissions = _permissions };
    void Because()
    {
        Build();
        _scopes.Add("injected:scope");
        _permissions.Add("injected:permission");
    }

    [Fact] void should_snapshot_scopes() => _result.Caller.Scopes.ShouldEqual(["fixture:read"]);
    [Fact] void should_snapshot_permissions() => _result.Caller.Permissions.ShouldEqual(["fixture:execute"]);
    [Fact] void should_not_expose_a_mutable_scope_set() => (_result.Caller.Scopes is HashSet<string>).ShouldBeFalse();
}
