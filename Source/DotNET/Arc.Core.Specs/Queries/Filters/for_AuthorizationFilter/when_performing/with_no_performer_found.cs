// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.Filters.for_AuthorizationFilter.when_performing;

public class with_no_performer_found : given.an_authorization_filter
{
    QueryResult _result;
    FullyQualifiedQueryName _queryName;

    void Establish()
    {
        _queryName = new FullyQualifiedQueryName("TestQuery");
        _queryPerformerProviders.TryGetPerformersFor(_queryName, out var _).Returns(false);

        _context = new QueryContext(_queryName, _correlationId, Paging.NotPaged, Sorting.None, null, []);
    }

    async Task Because() => _result = await _filter.OnPerform(_context);

    [Fact] void should_not_call_authorization_evaluator() => _authorizationEvaluator.DidNotReceive().IsAuthorized(Arg.Any<Type>());
    [Fact] void should_return_successful_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_be_authorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_not_have_exceptions() => _result.HasExceptions.ShouldBeFalse();
}