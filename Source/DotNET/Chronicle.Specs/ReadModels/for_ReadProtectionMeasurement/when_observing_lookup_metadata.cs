// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadProtectionMeasurement;

public class when_observing_lookup_metadata : given.a_measure_pipeline
{
    int _attributes;
    CommandResult _off;
    CommandResult _measure;

    void Establish() => _additionalCommands.Add(typeof(Redirected).FullName!);

    async Task Because()
    {
        Task Handle(CommandContext context)
        {
            CountedAttribute.Observer.Value = () => _attributes++;
            try
            {
                var parameter = typeof(CountedDependencies).GetMethod(nameof(CountedDependencies.Handle))!.GetParameters()[0];
                ReadModelDispatch.Register<Stock>();
                new ChronicleParameterDependencyResolver([typeof(Stock)]).TryResolve(_services, parameter, out _);
                ReadParameter(context, "stock");
                Stage(context);
            }
            finally
            {
                CountedAttribute.Observer.Value = null;
            }
            return Task.CompletedTask;
        }
        _off = await Run(false, Handle, new Redirected("other-key"));
        _measure = await Run(true, Handle, new Redirected("other-key"));
    }

    [Fact] void should_not_reinstantiate_application_attributes_for_measurement() => _attributes.ShouldEqual(2);
    [Fact] void should_report_the_convention_redirect_from_the_actual_key() => _telemetry.Select(_ => _.Category).ShouldContain("redirect");
    [Fact] void should_keep_result_and_status_inputs() => (_measure.IsSuccess, _measure.IsValid, _measure.HasExceptions).ShouldEqual((_off.IsSuccess, _off.IsValid, _off.HasExceptions));
    [Fact] void should_keep_appended_events_and_conditions() => _batches[1].ShouldEqual(_batches[0]);

    [ReadProtection(ReadProtectionMode.Measure)]
    public record Redirected(string Other);

    public class CountedDependencies
    {
        public void Handle([Counted] Stock stock) { }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class CountedAttribute : Attribute
    {
        public static readonly AsyncLocal<Action?> Observer = new();
        public CountedAttribute() => Observer.Value?.Invoke();
    }
}
