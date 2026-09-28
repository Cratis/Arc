// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.Filters;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_a_real_authorization_filter_and_no_declarations : given.a_query_pipeline
{
    QueryResult _result;
    bool _invoked;

    void Establish()
    {
        _serviceProvider.GetService(typeof(AuthorizationDeclarations)).Returns((object?)null);
        var name = new FullyQualifiedQueryName("ProtectedQuery.Load");
        _queryPerformer.Type.Returns(typeof(ProtectedQuery));
        _queryPerformer.Dependencies.Returns([]);
        _queryPerformer.Perform(Arg.Any<QueryContext>()).Returns(_ =>
        {
            _invoked = true;
            return ValueTask.FromResult<object?>(new ProtectedQuery());
        });
        _queryPerformerProviders.TryGetPerformersFor(name, out var _).Returns(call =>
        {
            call[1] = _queryPerformer;
            return true;
        });
        var filter = new AuthorizationFilter(_queryPerformerProviders);
        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(call => filter.OnPerform(call.Arg<QueryContext>()));
    }

    async Task Because() => _result = await _pipeline.Perform(
        new FullyQualifiedQueryName("ProtectedQuery.Load"), QueryArguments.Empty, Paging.NotPaged, Sorting.None, _serviceProvider);

    [Fact] void should_report_unauthorized_instead_of_an_error() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_report_the_configuration_failure() => _result.ExceptionMessages.ShouldContain("Authorization declarations are unavailable.");
    [Fact] void should_not_invoke_the_query() => _invoked.ShouldBeFalse();

    public record ProtectedQuery;
}
