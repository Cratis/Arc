// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerBuilder.when_building;

public class with_mutable_authentication_methods : given.a_builder
{
    readonly List<string> _methods = ["fixture-method"];

    void Establish() => _facts = _facts with { Assurance = _facts.Assurance! with { Amr = _methods } };
    void Because()
    {
        Build();
        _methods.Add("injected-method");
    }

    [Fact] void should_snapshot_assurance_methods() => _result.Caller.Assurance.Amr.SequenceEqual(["fixture-method"]).ShouldBeTrue();
}
