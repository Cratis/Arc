// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_enumeration_start_blocks_until_canceled : given.an_observable_query
{
    readonly CancellationBlockingEnumerable _enumerable = new(blockEnumeratorCreation: false);
    Exception? _exception;

    void Establish()
    {
        _state.Enumerable = _enumerable;
        _scenario.Services.AddSingleton<IAuthorizationPolicyRuntime>(new ChangingPrincipalRuntime());
        _scenario.Services.AddScoped<ObservableQueryScope>();
    }

    async Task Because()
    {
        var observe = Task.Run(() => _scenario.Observe(nameof(ObservableScenarioReadModel.ScopedAsync)));
        await _enumerable.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await _scenario.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        _exception = await Catch.Exception(() => observe);
    }

    [Fact] void should_cancel_startup() => _enumerable.Canceled.Task.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_dispose_the_enumerator_once() => _enumerable.EnumeratorDisposals.ShouldEqual(1);
    [Fact] void should_release_the_scope_once() => _state.Scope.DisposalCount.ShouldEqual(1);
    [Fact] void should_end_the_pending_observe() => (_exception is ObjectDisposedException or OperationCanceledException).ShouldBeTrue();
}
