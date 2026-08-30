// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_query_arguments_are_mutated_after_subscribing : given.a_guarded_sse_connection
{
    readonly NestedArgument _nested = new(["original"], new ArgumentId("concept-value"));
    QueryArguments _arguments;

    void Establish()
    {
        _arguments = new QueryArguments { ["filter"] = _nested };
        _queryContextManager.Current.Returns(new QueryContext(
            QueryName,
            CorrelationId.New(),
            Paging.NotPaged,
            Sorting.None,
            _arguments));
    }

    async Task Because() => await RunConnection(async () =>
    {
        _nested.Values[0] = "mutated";
        _nested.Values.Add("injected");
        _arguments["filter"] = new NestedArgument(["replacement"], new ArgumentId("replacement"));

        _subject.OnNext(["item-a"]);
        await WaitFor(() => _guardCalls.Count == 1);
    });

    [Fact] void should_guard_with_the_nested_values_captured_at_subscription_creation() =>
        ((NestedArgument)_guardCalls.Single().Arguments["filter"]).Values.ShouldEqual(["original"]);
    [Fact] void should_preserve_the_nested_concept_runtime_type() =>
        ((NestedArgument)_guardCalls.Single().Arguments["filter"]).Id.ShouldEqual(new ArgumentId("concept-value"));
    [Fact] void should_not_send_an_error() => HasErrorFor(FirstQueryId).ShouldBeFalse();

    public record ArgumentId(string Value) : ConceptAs<string>(Value);

    public record NestedArgument(List<string> Values, ArgumentId Id);
}
