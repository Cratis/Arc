// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Reactors.for_Refused;

public class when_matching_refusals : Specification
{
    CommandRefusal _constraint;
    bool _named;
    bool _wrongName;
    bool _anyConstraint;
    bool _anyAuthorization;
    bool _explicitAuthorization;

    void Establish() => _constraint = new(CommandRefusalKind.Constraint, "First", "refused") { ConstraintNames = ["First", "Second"] };

    void Because()
    {
        _named = Refused.ByConstraint("Second").Matches(_constraint);
        _wrongName = Refused.ByConstraint("second").Matches(_constraint);
        _anyConstraint = Refused.ByAnyConstraint.Matches(new(CommandRefusalKind.Constraint, null, "refused"));
        _anyAuthorization = Refused.Any.Matches(new(CommandRefusalKind.Authorization, null, "denied"));
        _explicitAuthorization = Refused.ByAuthorization.Matches(new(CommandRefusalKind.Authorization, null, "denied"));
    }

    [Fact] void should_match_any_violated_name() => _named.ShouldBeTrue();
    [Fact] void should_match_names_case_sensitively() => _wrongName.ShouldBeFalse();
    [Fact] void should_match_an_unnamed_constraint_explicitly() => _anyConstraint.ShouldBeTrue();
    [Fact] void should_exclude_authorization_from_any() => _anyAuthorization.ShouldBeFalse();
    [Fact] void should_match_authorization_explicitly() => _explicitAuthorization.ShouldBeTrue();
}
