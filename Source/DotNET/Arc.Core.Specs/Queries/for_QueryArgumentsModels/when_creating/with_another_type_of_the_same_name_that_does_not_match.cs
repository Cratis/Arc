// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryArgumentsModels.when_creating;

/// <summary>
/// Two unrelated types can carry the same simple name, which makes the conventional name ambiguous. Resolution has
/// to consider all of them and pick the one that actually covers the query's arguments — settling for whichever the
/// runtime happens to report first would resolve differently from run to run, and silently skip validation whenever
/// the wrong one won.
/// </summary>
public class with_another_type_of_the_same_name_that_does_not_match : given.query_arguments_models
{
    bool _result;
    object _model;

    void Establish() => ForQuery("Lookup", new QueryParameter("email", typeof(string)));

    void Because() => _result = _models.TryCreateFor(_performer, ArgumentsOf(("email", "author@cratis.io")), out _model);

    [Fact] void should_resolve_a_model() => _result.ShouldBeTrue();
    [Fact] void should_pick_the_type_covering_the_arguments() => _model.ShouldBeOfExactType<ambiguous.alpha.LookupParameters>();
    [Fact] void should_fill_it_from_the_arguments() => ((ambiguous.alpha.LookupParameters)_model).Email.ShouldEqual("author@cratis.io");
}
