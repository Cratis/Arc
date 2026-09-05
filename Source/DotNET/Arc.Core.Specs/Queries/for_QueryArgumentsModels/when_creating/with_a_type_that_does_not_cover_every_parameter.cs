// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryArgumentsModels.when_creating;

/// <summary>
/// The name alone is not enough to claim a type models a query's arguments — a type that happens to carry the name
/// but does not cover every parameter would silently validate the wrong shape and let the uncovered arguments
/// through unchecked.
/// </summary>
public class with_a_type_that_does_not_cover_every_parameter : given.query_arguments_models
{
    bool _result;
    object _model;

    void Establish() => ForQuery("SearchByMismatch", new QueryParameter("email", typeof(string)));

    void Because() => _result = _models.TryCreateFor(_performer, ArgumentsOf(("email", "author@cratis.io")), out _model);

    [Fact] void should_not_resolve_a_model() => _result.ShouldBeFalse();
    [Fact] void should_not_produce_an_instance() => _model.ShouldBeNull();
}
