// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.for_UnavailableParts;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability.for_AvailableParts;

public class when_observing_one_part_with_arguments : given_a_part_gated_on_an_argument
{
    string[] _forOther = [];
    string[] _forAllowed = ["not answered"];

    async Task Because()
    {
        var parts = _scope.ServiceProvider.GetRequiredService<IUnavailableParts>();
        _forOther = await FirstAnswer(AvailableParts.ObserveUnavailable(parts, typeof(UngatedCommand).FullName, "{\"vesselId\":\"other\"}"));
        _forAllowed = await FirstAnswer(AvailableParts.ObserveUnavailable(parts, typeof(UngatedCommand).FullName, "{\"vesselId\":\"allowed\"}"));
    }

    [Fact] void should_list_the_part_for_the_argument_the_gate_refuses() => _forOther.ShouldContainOnly(typeof(UngatedCommand).FullName);
    [Fact] void should_not_list_the_part_for_the_argument_the_gate_accepts() => _forAllowed.ShouldBeEmpty();

    static async Task<string[]> FirstAnswer(System.Reactive.Subjects.ISubject<AvailableParts> subject)
    {
        var answer = new TaskCompletionSource<string[]>();
        using var subscription = subject.Subscribe(parts => answer.TrySetResult([.. parts.Unavailable.Select(part => part.Name)]));
        try
        {
            return await answer.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            (subject as IDisposable)?.Dispose();
        }
    }
}
