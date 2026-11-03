// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class and_authorization_rechecking_swallows_the_snapshot_denial : given.a_query_pipeline
{
    QueryResult _result;
    IDisposable _boundary;
    bool _filtersCompleted;
    bool _recheckedAuthorization;
    bool _caughtDenial;

    void Establish()
    {
        _boundary = TenantIdAccessor.Independent.BeginRequest();
        var resolver = Substitute.For<ITenantIdResolver>();
        resolver.Resolve().Returns("selected");
        var membership = Substitute.For<ITenantMembership>();
        membership.IsMember(Arg.Any<ClaimsPrincipal>(), Arg.Any<TenantId>()).Returns(false);
        var requests = Substitute.For<IHttpRequestContextAccessor>();
        var request = Substitute.For<IHttpRequestContext>();
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IEnumerable<ITenantMembership>)).Returns(new[] { membership });
        request.RequestServices.Returns(services);
        request.User.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user-1")], "test")));
        requests.Current.Returns(request);
        var principals = new CurrentPrincipalAccessor(requests);
        var verification = new TenantMembershipVerification(requests, principals, NullLogger<TenantMembershipVerification>.Instance);
        var accessor = new TenantIdAccessor(resolver, verification);
        _serviceProvider.GetService(typeof(ICurrentPrincipalAccessor)).Returns(principals);
        _serviceProvider.GetService(typeof(TenantIdAccessor)).Returns(accessor);
        _serviceProvider.GetService(typeof(ITenantIdAccessor)).Returns(accessor);
        _serviceProvider.GetService(typeof(ITenantIdResolver)).Returns(resolver);
        _queryPerformerProviders.TryGetPerformersFor(Arg.Any<FullyQualifiedQueryName>(), out Arg.Any<IQueryPerformer>())
            .Returns(call =>
            {
                call[1] = _queryPerformer;
                return true;
            });
        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(_ =>
        {
            _filtersCompleted = true;
            return QueryResult.Success(_correlationId);
        });
        var evaluator = Substitute.For<IAuthorizationAttributeEvaluator>();
        evaluator.GetAuthorizationRequirements(Arg.Any<Type>()).Returns(call =>
        {
            // The final declaration lookup happens after the post-filter membership guard.
            if (_filtersCompleted)
            {
                _recheckedAuthorization = true;
                try
                {
                    _ = OperationSnapshot.Current!.Tenant;
                }
                catch (TenantMembershipDenied)
                {
                    _caughtDenial = true;
                }
            }
            return Array.Empty<AuthorizationRequirement>();
        });
        _serviceProvider.GetService(typeof(AuthorizationDeclarations)).Returns(new AuthorizationDeclarations(
            new KnownInstancesOf<IAnonymousEvaluator>([]),
            new KnownInstancesOf<IAuthorizationAttributeEvaluator>([evaluator])));
    }

    async Task Because() => _result = await _pipeline.Perform("test", new QueryArguments(), Paging.NotPaged, Sorting.None, _serviceProvider);

    void Destroy() => _boundary.Dispose();

    [Fact] void should_recheck_authorization_after_the_filters() => _recheckedAuthorization.ShouldBeTrue();
    [Fact] void should_catch_the_snapshot_denial() => _caughtDenial.ShouldBeTrue();
    [Fact] void should_be_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_perform_the_query() => _queryPerformer.DidNotReceive().Perform(Arg.Any<QueryContext>());
}
