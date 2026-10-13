// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Chronicle.Testing.Queries;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario.when_observing;

public class with_an_initial_null_key_pin : given.an_observed_account
{
    Exception? _error;

    void Establish() => _scenario.Given.ForEventSource(_id).ReadModel(new ObservedAccount(null!, 42m));

    void Because()
    {
        using var services = _scenario.Services.BuildServiceProvider();
        _error = Catch.Exception(() => services.GetRequiredService<IReadModels>().Observe<ObservedAccount>());
    }

    [Fact] void should_refuse_the_null_key_value() => _error.ShouldBeOfExactType<PinnedReadModelCannotBeObserved>();
    [Fact] void should_name_the_pinned_type() => _error!.Message.Contains(typeof(ObservedAccount).FullName!, StringComparison.Ordinal).ShouldBeTrue();
}
