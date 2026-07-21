// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryArgumentsModels.when_creating;

/// <summary>
/// The read-model-prefixed name is the second form the proxy generator accepts, and it exists so two read models can
/// each model a query of the same name. Both sides have to resolve the same type or they would validate differently.
/// </summary>
public class with_the_read_model_prefixed_name : given.query_arguments_models
{
    bool _result;
    object _model;

    void Establish() => ForQuery("SearchByPrefixedName", new QueryParameter("name", typeof(string)));

    void Because() => _result = _models.TryCreateFor(_performer, ArgumentsOf(("name", "Ada")), out _model);

    [Fact] void should_resolve_a_model() => _result.ShouldBeTrue();
    [Fact] void should_resolve_the_prefixed_type() => _model.ShouldBeOfExactType<SearchReadModelSearchByPrefixedNameParameters>();
}
