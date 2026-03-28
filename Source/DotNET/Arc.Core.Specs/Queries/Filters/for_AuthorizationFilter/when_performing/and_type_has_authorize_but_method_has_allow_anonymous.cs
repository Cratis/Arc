// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.Filters.for_AuthorizationFilter.when_performing;

public class and_type_has_authorize_but_method_has_allow_anonymous : given.an_authorization_filter
{
    QueryResult _result;
    FullyQualifiedQueryName _queryName;

    void Establish()
    {
        _queryName = new FullyQualifiedQueryName("TestQuery");
        _queryPerformer.Type.Returns(typeof(object));
        _queryPerformer.AllowsAnonymousAccess.Returns(true);
        _queryPerformerProviders.TryGetPerformersFor(_queryName, out Arg.Any<IQueryPerformer?>()).Returns(callInfo =>
        {
            callInfo[1] = _queryPerformer;
            return true;
        });

        _context = new QueryContext(_queryName, _correlationId, Paging.NotPaged, Sorting.None, null, []);

        // Method-level authorization check passes ([AllowAnonymous] on the method overrides type-level [Authorize])
        _queryPerformer.IsAuthorized(_context).Returns(true);
    }

    async Task Because() => _result = await _filter.OnPerform(_context);

    [Fact] void should_not_call_type_level_authorization_evaluator() => _authorizationEvaluator.DidNotReceive().IsAuthorized(typeof(object));
    [Fact] void should_call_is_authorized_on_performer() => _queryPerformer.Received(1).IsAuthorized(_context);
    [Fact] void should_return_successful_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_be_authorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_not_have_exceptions() => _result.HasExceptions.ShouldBeFalse();
}
