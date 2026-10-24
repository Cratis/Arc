// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_query_policy_resolution_fails : given.a_guarded_sse_connection
{
    Exception _failure;
    IAuthorizationPolicyRuntime _runtime;

    void Establish()
    {
        _queryIds = [];

        // Keep the custom pipeline seam free of asynchronous declarations. The root container
        // still resolves the real guest declaration during the hub's endpoint metadata probe.
        _admissionServices = new ServiceCollection().AddSingleton(_fallbackAuthorization).BuildServiceProvider();
        _queryPipeline.Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<QueryResult>(_failure));
    }

    protected override void ConfigureGuards(IServiceCollection services, List<Type> guardTypes)
    {
        ConfigureGuestQuery(services);
        _runtime = Substitute.For<IAuthorizationPolicyRuntime>();
        _runtime.Resolve(Arg.Any<IReadOnlyList<AuthorizationRequirement>>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<IAuthorizationPolicyResolution>(_failure));
        services.AddSingleton(_runtime);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task should_preserve_failure_outcomes_for_both_orders_and_a_warm_cache(bool providerFailsFirst)
    {
        var providerFailure = new PolicyProviderUnavailable();
        var configurationFailure = new InvalidAuthorizationConfiguration("Unknown policy");
        Exception[] failures = providerFailsFirst
            ? [providerFailure, configurationFailure, providerFailure]
            : [configurationFailure, providerFailure, providerFailure];

        await RunConnection(async () =>
        {
            for (var index = 0; index < failures.Length; index++)
            {
                _failure = failures[index];
                var queryId = $"failed-{index}";
                await _hub.HandleSSESubscribe(CreateSubscribeContext(queryId));
                HasErrorFor(queryId).ShouldEqual(_failure is PolicyProviderUnavailable);
                HasUnauthorizedFor(queryId).ShouldEqual(_failure is InvalidAuthorizationConfiguration);
                CountQueryResultsFor(queryId).ShouldEqual(0);
                _subscribeStatusCodes[queryId].ShouldEqual(_failure is InvalidAuthorizationConfiguration ? 401 : 200);
            }
        });

        // Unknown configuration caches false; subsequent provider failures must still fail in
        // the pipeline. A cold provider failure must never be cached as anonymous admission.
        await _runtime.Received(providerFailsFirst ? 2 : 1).Resolve(
            Arg.Any<IReadOnlyList<AuthorizationRequirement>>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>());
        _healthTracker.DidNotReceiveWithAnyArgs().RegisterSubscription(default!, default!, default!);
    }

    class PolicyProviderUnavailable : Exception;
}
