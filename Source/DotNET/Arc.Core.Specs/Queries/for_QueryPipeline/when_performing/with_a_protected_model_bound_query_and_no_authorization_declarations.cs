// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_a_protected_model_bound_query_and_no_authorization_declarations : given.a_query_pipeline
{
    QueryResult _result;
    bool _performed;

    void Establish()
    {
        _serviceProvider.GetService(typeof(AuthorizationDeclarations)).Returns((object?)null);
        var method = typeof(ProtectedQuery).GetMethod(nameof(ProtectedQuery.Load))!;
        var performer = new ModelBoundQueryPerformer(
            typeof(ProtectedQuery),
            typeof(ProtectedQuery).FullName!,
            method,
            Substitute.For<IServiceProviderIsService>(),
            Substitute.For<IAuthorizationEvaluator>());
        _queryPerformerProviders.TryGetPerformersFor(performer.FullyQualifiedName, out var _).Returns(call =>
        {
            call[1] = performer;
            return true;
        });
        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(QueryResult.Success(_correlationId));
    }

    async Task Because()
    {
        ProtectedQuery.OnPerform = () => _performed = true;
        try
        {
            _result = await _pipeline.Perform(
                (FullyQualifiedQueryName)$"{typeof(ProtectedQuery).FullName}.Load",
                QueryArguments.Empty,
                Paging.NotPaged,
                Sorting.None,
                _serviceProvider);
        }
        finally
        {
            ProtectedQuery.OnPerform = null;
        }
    }

    [Fact] void should_report_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_invoke_the_query() => _performed.ShouldBeFalse();

    [Authorize(Policy = "Allow")]
    public record ProtectedQuery
    {
        public static Action? OnPerform { get; set; }
        public static ProtectedQuery Load()
        {
            OnPerform?.Invoke();
            return new();
        }
    }
}
