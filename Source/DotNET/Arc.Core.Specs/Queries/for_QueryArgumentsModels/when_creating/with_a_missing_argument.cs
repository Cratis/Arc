// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryArgumentsModels.when_creating;

/// <summary>
/// An absent argument leaves its member at the type's default rather than preventing the model from being built —
/// the point is to hand the validator something to reject, not to fail before it can.
/// </summary>
public class with_a_missing_argument : given.query_arguments_models
{
    bool _result;
    object _model;

    void Establish() => ForQuery("SearchByEmail", new QueryParameter("email", typeof(string)), new QueryParameter("minAge", typeof(int)));

    void Because() => _result = _models.TryCreateFor(_performer, ArgumentsOf(("minAge", 21)), out _model);

    [Fact] void should_still_resolve_a_model() => _result.ShouldBeTrue();
    [Fact] void should_leave_the_absent_member_at_its_default() => ((SearchByEmailParameters)_model).Email.ShouldEqual(string.Empty);
    [Fact] void should_fill_the_supplied_member() => ((SearchByEmailParameters)_model).MinAge.ShouldEqual(21);
}
