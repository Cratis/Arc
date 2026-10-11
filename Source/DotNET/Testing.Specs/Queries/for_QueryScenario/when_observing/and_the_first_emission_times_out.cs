// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_the_first_emission_times_out : given.an_observable_query
{
    readonly Subject<ObservableScenarioReadModel> _subject = new();
    Exception? _exception;

    void Establish() => _state.Subject = _subject;

    async Task Because() => _exception = await Catch.Exception(() => _scenario.Observe(nameof(ObservableScenarioReadModel.Current), timeout: TimeSpan.FromMilliseconds(50)));

    [Fact] void should_report_the_timeout() => _exception.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_release_the_subscription() => _subject.HasObservers.ShouldBeFalse();
}
