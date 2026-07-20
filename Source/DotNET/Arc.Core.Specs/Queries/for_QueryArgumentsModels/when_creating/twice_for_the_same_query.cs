// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryArgumentsModels.when_creating;

/// <summary>
/// Resolution is cached per query because it scans an assembly. The cache must hold the resolved type and not the
/// materialized instance — sharing one instance across requests would leak one caller's arguments into another's.
/// </summary>
public class twice_for_the_same_query : given.query_arguments_models
{
    object _first;
    object _second;

    void Establish() => ForQuery("SearchByEmail", new QueryParameter("email", typeof(string)), new QueryParameter("minAge", typeof(int)));

    void Because()
    {
        _models.TryCreateFor(_performer, ArgumentsOf(("email", "first@cratis.io"), ("minAge", 1)), out _first);
        _models.TryCreateFor(_performer, ArgumentsOf(("email", "second@cratis.io"), ("minAge", 2)), out _second);
    }

    [Fact] void should_resolve_the_same_type() => _second.GetType().ShouldEqual(_first.GetType());
    [Fact] void should_not_reuse_the_instance() => ReferenceEquals(_first, _second).ShouldBeFalse();
    [Fact] void should_fill_the_second_from_its_own_arguments() => ((SearchByEmailParameters)_second).Email.ShouldEqual("second@cratis.io");
    [Fact] void should_leave_the_first_untouched() => ((SearchByEmailParameters)_first).Email.ShouldEqual("first@cratis.io");
}
