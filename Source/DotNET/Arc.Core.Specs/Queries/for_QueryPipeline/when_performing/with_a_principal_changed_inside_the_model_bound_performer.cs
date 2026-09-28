// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_a_principal_changed_inside_the_model_bound_performer : given.a_query_pipeline
{
    QueryResult _result;
    bool _invoked;
    bool _performerEntered;
    System.Diagnostics.ActivitySource _source;

    void Establish()
    {
        var initial = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "original")], "test"));
        var replacement = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "replacement")], "test"));
        var current = initial;
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(_ => current);
        var declarations = new AuthorizationDeclarations(
            new KnownInstancesOf<IAnonymousEvaluator>([]),
            new KnownInstancesOf<IAuthorizationAttributeEvaluator>([new AuthorizationAttributeEvaluator()]));
        _serviceProvider.GetService(typeof(AuthorizationDeclarations)).Returns(declarations);
        _serviceProvider.GetService(typeof(ICurrentPrincipalAccessor)).Returns(accessor);
        var method = typeof(ProtectedQuery).GetMethod(nameof(ProtectedQuery.Load))!;
        var model = new ModelBoundQueryPerformer(
            typeof(ProtectedQuery),
            typeof(ProtectedQuery).FullName!,
            method,
            Substitute.For<IServiceProviderIsService>(),
            Substitute.For<IAuthorizationEvaluator>());
        _queryPerformer.Type.Returns(typeof(ProtectedQuery));
        _queryPerformer.Name.Returns((QueryName)"Load");
        _queryPerformer.Dependencies.Returns([]);
        _queryPerformer.Perform(Arg.Any<QueryContext>()).Returns(call =>
        {
            _performerEntered = true;
            current = replacement;
            return model.Perform(call.Arg<QueryContext>());
        });
        _queryPerformerProviders.TryGetPerformersFor(model.FullyQualifiedName, out var _).Returns(call =>
        {
            call[1] = _queryPerformer;
            return true;
        });
        var filters = Substitute.For<IStagedQueryFilters>();
        filters.Authorize(Arg.Any<QueryContext>()).Returns(call =>
        {
            var context = call.Arg<QueryContext>();
            context.AuthorizedExecution = new AuthorizedExecution(method, AuthorizationPrincipalIdentity.Capture(initial), declarations.For(method), false);
            return Task.FromResult(QueryResult.Success(context.CorrelationId));
        });
        filters.AfterAuthorization(Arg.Any<QueryContext>()).Returns(call => Task.FromResult(QueryResult.Success(call.Arg<QueryContext>().CorrelationId)));
        var activity = Substitute.For<IActivitySource<QueryPipeline>>();
        _source = new System.Diagnostics.ActivitySource("Cratis.Arc.Test");
        activity.ActualSource.Returns(_source);
        _pipeline = new QueryPipeline(
            _correlationIdAccessor,
            _queryContextManager,
            filters,
            _queryPerformerProviders,
            _queryRenderers,
            _readModelInterceptors,
            _discoverableValidators,
            activity);
    }

    async Task Because()
    {
        ProtectedQuery.OnInvoke = () => _invoked = true;
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
            ProtectedQuery.OnInvoke = null;
        }
    }

    void Cleanup() => _source.Dispose();

    [Fact] void should_reach_the_performer_after_the_pipeline_check() => _performerEntered.ShouldBeTrue();
    [Fact] void should_map_the_performers_identity_exception_to_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_invoke_the_query_method() => _invoked.ShouldBeFalse();

    [Authorize(Policy = "Allow")]
    public record ProtectedQuery
    {
        public static Action? OnInvoke { get; set; }
        public static ProtectedQuery Load()
        {
            OnInvoke?.Invoke();
            return new();
        }
    }
}
