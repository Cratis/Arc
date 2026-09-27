// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_staged_filter_supplied_subscription_scope : given.a_query_pipeline
{
    readonly FullyQualifiedQueryName _name = "ScopedObservable";
    readonly List<string> _membership = ["after-authorization"];
    QueryContext _authorizedContext;
    QueryContext _afterAuthorizationContext;
    QueryResult _result;
    string _scopeReceivedAfterAuthorization;

    void Establish()
    {
        var stagedFilters = Substitute.For<IStagedQueryFilters>();
        stagedFilters.Authorize(Arg.Any<QueryContext>()).Returns(call =>
        {
            _authorizedContext = call.Arg<QueryContext>();
            _authorizedContext.SubscriptionScope = new List<string> { "authorized" };
            return Task.FromResult(QueryResult.Success(_correlationId));
        });
        stagedFilters.AfterAuthorization(Arg.Any<QueryContext>()).Returns(call =>
        {
            _afterAuthorizationContext = call.Arg<QueryContext>();
            _scopeReceivedAfterAuthorization = ((List<string>)_afterAuthorizationContext.SubscriptionScope!)[0];
            _afterAuthorizationContext.SubscriptionScope = _membership;
            return Task.FromResult(QueryResult.Success(_correlationId));
        });
        query_filters = stagedFilters;
        var activitySource = Substitute.For<Cratis.Traces.IActivitySource<QueryPipeline>>();
        activitySource.ActualSource.Returns(new System.Diagnostics.ActivitySource("Cratis.Arc.Test.StagedScope"));
        _pipeline = new QueryPipeline(
            _correlationIdAccessor,
            _queryContextManager,
            query_filters,
            _queryPerformerProviders,
            _queryRenderers,
            _readModelInterceptors,
            _discoverableValidators,
            activitySource);
        _queryPerformer.Dependencies.Returns([]);
        _queryPerformerProviders.TryGetPerformersFor(_name, out var _).Returns(call =>
        {
            call[1] = _queryPerformer;
            return true;
        });
        _queryPerformer.Perform(Arg.Any<QueryContext>()).Returns(_ =>
        {
            _membership[0] = "changed-during-execution";
            return ValueTask.FromResult<object?>(null);
        });
    }

    async Task Because() => _result = await _pipeline.Perform(_name, new QueryArguments(), Paging.NotPaged, Sorting.None, _serviceProvider);

    [Fact] void should_pass_a_copied_context_between_stages() => ReferenceEquals(_authorizedContext, _afterAuthorizationContext).ShouldBeFalse();
    [Fact] void should_propagate_the_authorized_scope_to_the_second_stage() => _scopeReceivedAfterAuthorization.ShouldEqual("authorized");
    [Fact] void should_capture_the_scope_from_the_second_stage_before_execution() =>
        ((List<string>)_result.AuthorizedQueryContext!.CreateSubscriptionScope()!)[0].ShouldEqual("after-authorization");
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
}
