// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryArgumentsModels.when_creating;

/// <summary>
/// Matching on name alone would accept a model whose members cannot hold the arguments, which then throws while
/// being materialized. Requiring the type means such a candidate is simply not the argument model, and validation
/// falls back to each argument on its own. It is also the criterion the proxy generator applies, so both sides
/// resolve the same type.
/// </summary>
public class with_a_type_whose_property_types_do_not_match : given.query_arguments_models
{
    bool _result;
    object _model;

    void Establish() => ForQuery("SearchByEmail", new QueryParameter("email", typeof(Guid)), new QueryParameter("minAge", typeof(int)));

    void Because() => _result = _models.TryCreateFor(_performer, ArgumentsOf(("email", Guid.NewGuid()), ("minAge", 21)), out _model);

    [Fact] void should_not_resolve_a_model() => _result.ShouldBeFalse();
    [Fact] void should_not_produce_an_instance() => _model.ShouldBeNull();
}
