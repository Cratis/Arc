// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryArgumentsModels.when_creating;

public class with_settable_properties : given.query_arguments_models
{
    bool _result;
    object _model;

    void Establish() => ForQuery("SearchByEmail", new QueryParameter("email", typeof(string)), new QueryParameter("minAge", typeof(int)));

    void Because() => _result = _models.TryCreateFor(_performer, ArgumentsOf(("email", "author@cratis.io"), ("minAge", 21)), out _model);

    [Fact] void should_resolve_a_model() => _result.ShouldBeTrue();
    [Fact] void should_be_of_the_matching_type() => _model.ShouldBeOfExactType<SearchByEmailParameters>();
    [Fact] void should_fill_the_string_argument() => ((SearchByEmailParameters)_model).Email.ShouldEqual("author@cratis.io");
    [Fact] void should_fill_the_numeric_argument() => ((SearchByEmailParameters)_model).MinAge.ShouldEqual(21);
}
