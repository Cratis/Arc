// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_scalar_coercion : given.a_query_pipeline
{
    enum Status
    {
        Active = 0,
        Inactive = 1
    }

    void Configure(string name, Type type)
    {
        var queryName = new FullyQualifiedQueryName("Rates.ByValue");
        _queryPerformer.FullyQualifiedName.Returns(queryName);
        _queryPerformer.Parameters.Returns(new QueryParameters([new QueryParameter(name, type)]));
        _queryPerformer.Dependencies.Returns([]);
        _queryPerformerProviders.TryGetPerformersFor(queryName, out var _).Returns(callInfo =>
        {
            callInfo[1] = _queryPerformer;
            return true;
        });
        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(QueryResult.Success(_correlationId));
    }

    [Theory]
    [InlineData("count", typeof(int), "abc")]
    [InlineData("id", typeof(Guid), "not-a-guid")]
    [InlineData("date", typeof(DateOnly), "not-a-date")]
    [InlineData("state", typeof(Status), "not-a-status")]
    [InlineData("enabled", typeof(bool), "not-a-bool")]
    async Task should_reject_invalid_values_without_invoking_the_performer(string name, Type type, string value)
    {
        Configure(name, type);
        var result = await _pipeline.Perform("Rates.ByValue", new QueryArguments { [name] = value }, Paging.NotPaged, Sorting.None, _serviceProvider);
        result.IsValid.ShouldBeFalse();
        result.ValidationResults.Single().Members.ShouldContainOnly(name);
        result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.MalformedRequest);
        result.HasExceptions.ShouldBeFalse();
        await _queryPerformer.DidNotReceive().Perform(Arg.Any<QueryContext>());
    }

    [Fact]
    async Task should_pass_converted_valid_values_to_the_performer()
    {
        Configure("count", typeof(int));
        _queryPerformer.Perform(Arg.Any<QueryContext>()).Returns(ValueTask.FromResult<object?>(null));
        var result = await _pipeline.Perform("Rates.ByValue", new QueryArguments { ["count"] = "42" }, Paging.NotPaged, Sorting.None, _serviceProvider);
        result.IsSuccess.ShouldBeTrue();
        await _queryPerformer.Received(1).Perform(Arg.Is<QueryContext>(context => (int)context.Arguments["count"] == 42));
    }
}
