// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_QueryScenario.given;

public class a_blocked_emission_guard : an_observable_query
{
    protected readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected bool _blockInitial;
    protected IObservableQueryEmissionGuards _guards = default!;

    void Establish()
    {
        _guards = Substitute.For<IObservableQueryEmissionGuards>();
        _guards.HasGuards.Returns(true);
        _guards.Guard(Arg.Any<ObservableQueryEmissionContext>()).Returns(async call =>
        {
            var context = (ObservableQueryEmissionContext)call[0];
            if (context.IsFirstEmission && !_blockInitial)
            {
                return ObservableQueryEmissionVerdict.Allow;
            }

            _entered.TrySetResult();
            await _release.Task.WaitAsync(context.CancellationToken);
            return ObservableQueryEmissionVerdict.Allow;
        });
        _scenario.Services.AddSingleton(_guards);
    }
}
