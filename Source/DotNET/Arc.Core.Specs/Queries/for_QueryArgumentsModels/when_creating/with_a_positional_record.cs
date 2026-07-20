// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryArgumentsModels.when_creating;

/// <summary>
/// A record is the idiomatic way to express a model in this codebase, so the argument set has to materialize through
/// a constructor and not only through settable properties.
/// </summary>
public class with_a_positional_record : given.query_arguments_models
{
    bool _result;
    object _model;

    void Establish() => ForQuery("SearchByName", new QueryParameter("name", typeof(string)), new QueryParameter("minAge", typeof(int)));

    void Because() => _result = _models.TryCreateFor(_performer, ArgumentsOf(("name", "Ada"), ("minAge", 36)), out _model);

    [Fact] void should_resolve_a_model() => _result.ShouldBeTrue();
    [Fact] void should_fill_the_first_positional_member() => ((SearchByNameParameters)_model).Name.ShouldEqual("Ada");
    [Fact] void should_fill_the_second_positional_member() => ((SearchByNameParameters)_model).MinAge.ShouldEqual(36);
}
