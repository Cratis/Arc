// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadProtectionMeasurement;

public class when_a_model_has_a_custom_registration : given.a_measure_pipeline
{
    readonly Stock _custom = new("custom-key");
    readonly List<object> _values = [];
    int _factories;
    CommandResult _off;
    CommandResult _measure;
    IReadModels _stored;

    void Establish()
    {
        _additionalCommands.Add(typeof(CustomProvide).FullName!);
        _stored = Substitute.For<IReadModels>();
        _services.GetService(typeof(IReadModels)).Returns(_stored);
        ReadModelDispatch.Register<Stock>();
        _services.GetService(typeof(IInstancesOf<ICanResolveParameterDependency>)).Returns(new KnownInstancesOf<ICanResolveParameterDependency>([new ChronicleParameterDependencyResolver([typeof(Stock)])]));
        _services.GetService(typeof(Stock)).Returns(_ =>
        {
            _factories++;
            return _custom;
        });
    }

    async Task Because()
    {
        async Task Handle(CommandContext context)
        {
            _values.AddRange(await new CommandProvideInvoker().Invoke(context, _services));
            Stage(context);
        }
        _off = await Run(false, Handle, new CustomProvide());
        Assert.False(_off.HasExceptions, string.Join(Environment.NewLine, _off.ExceptionMessages) + _off.ExceptionStackTrace);
        _measure = await Run(true, Handle, new CustomProvide());
        Assert.False(_measure.HasExceptions, string.Join(Environment.NewLine, _measure.ExceptionMessages) + _measure.ExceptionStackTrace);
    }

    [Fact] void should_keep_the_custom_value_in_both_modes() => _values.ShouldEqual(new object[] { _custom, _custom });
    [Fact] void should_call_the_factory_once_per_invocation() => _factories.ShouldEqual(2);
    [Fact] void should_not_replace_the_factory_with_a_stored_read() => _stored.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_keep_result_and_status_inputs() => (_measure.IsSuccess, _measure.IsValid, _measure.HasExceptions).ShouldEqual((_off.IsSuccess, _off.IsValid, _off.HasExceptions));
    [Fact] void should_keep_appended_events_and_conditions() => _batches[1].ShouldEqual(_batches[0]);

    [ReadProtection(ReadProtectionMode.Measure)]
    public record CustomProvide
    {
        public Stock Provide(Stock stock) => stock;
    }
}
