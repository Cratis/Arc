// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryArgumentsModels.when_creating;

/// <summary>
/// "Covers every parameter" is vacuously true for a query that takes none, which would let a parameterless query
/// bind any same-named type that happens to exist and validate a shape the developer never associated with it.
/// </summary>
public class without_any_parameters : given.query_arguments_models
{
    bool _result;
    object _model;

    void Establish() => ForQuery("SearchByEmail");

    void Because() => _result = _models.TryCreateFor(_performer, ArgumentsOf(), out _model);

    [Fact] void should_not_resolve_a_model() => _result.ShouldBeFalse();
    [Fact] void should_not_produce_an_instance() => _model.ShouldBeNull();
}
