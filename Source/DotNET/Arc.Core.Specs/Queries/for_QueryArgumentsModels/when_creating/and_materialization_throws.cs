// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryArgumentsModels.when_creating;

/// <summary>
/// Materializing a model runs a caller-supplied argument set through reflection and a constructor, which can throw —
/// a record with a guard clause is idiomatic here. Letting that escape would turn hostile input into a server error,
/// the exact failure mode the shared validator avoids for a throwing validator.
/// </summary>
public class and_materialization_throws : given.query_arguments_models
{
    bool _result;
    object _model;
    Exception _error;

    void Establish() => ForQuery("SearchByGuarded", new QueryParameter("name", typeof(string)));

    void Because() => _error = Catch.Exception(() => _result = _models.TryCreateFor(_performer, ArgumentsOf(("name", "anything")), out _model));

    [Fact] void should_not_let_the_exception_escape() => _error.ShouldBeNull();
    [Fact] void should_report_that_no_model_was_created() => _result.ShouldBeFalse();
    [Fact] void should_not_produce_an_instance() => _model.ShouldBeNull();
}
