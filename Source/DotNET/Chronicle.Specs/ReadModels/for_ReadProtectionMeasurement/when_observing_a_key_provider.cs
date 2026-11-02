// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadProtectionMeasurement;

public class when_observing_a_key_provider : given.a_measure_pipeline
{
    CommandResult _result;

    void Establish() => _additionalCommands.Add(typeof(Redirected).FullName!);

    async Task Because() => _result = await Run(
        true,
        context =>
        {
            var parameter = typeof(Dependencies).GetMethod(nameof(Dependencies.Handle))!.GetParameters()[0];
            ReadProtectionMeasurement.Current!.Parameter(parameter, true);
            ReadProtectionMeasurement.Current.StoredRead(typeof(Stock), (ReadModelKey)"subject");
            Stage(context);
            return Task.CompletedTask;
        },
        new Redirected("other-key"));

    [Fact] void should_complete_the_command() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_observe_a_convention_redirect() => _telemetry.Select(entry => entry.Category).ShouldNotContain("redirect");

    [ReadProtection(ReadProtectionMode.Measure)]
    public record Redirected(string Other);

    public class Dependencies
    {
        public void Handle([LookUpBy<CallerSubject>] Stock stock) { }
    }
}
