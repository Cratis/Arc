// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_event_registration_is_runtime_only : Specification
{
    IReadModels _readModels;
    EventSourceId _id;
    ServiceProvider _provider;
    IServiceScope _scope;
    ChronicleReadModelForCommandResolver _resolver;
    Dictionary<Type, object?> _resolved;
    InterfaceBalance _interfaceReleased;
    BaseBalance _baseReleased;
    ExplicitBalance _explicitReleased;

    void Establish()
    {
        _id = EventSourceId.New();
        _readModels = Substitute.For<IReadModels>();
        _interfaceReleased = new(2);
        _baseReleased = new(2);
        _explicitReleased = new(2);
        Configure(new InterfaceBalance(1), _interfaceReleased);
        Configure(new BaseBalance(1), _baseReleased);
        Configure(new ExplicitBalance(1), _explicitReleased);
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(ExplicitAccountChanged), typeof(DerivedAccountChanged)]);
        artifacts.Reducers.Returns([typeof(InterfaceBalance), typeof(BaseBalance), typeof(ExplicitBalance), typeof(NominatedReducer)]);
        var services = new ServiceCollection().AddReadModels(artifacts).AddSingleton(_readModels);
        var values = new CommandContextValues { { WellKnownCommandContextKeys.EventSourceId, _id } };
        services.AddScoped(_ => new CommandContext(CorrelationId.New(), typeof(object), new object(), [], values, null));
        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        _resolver = _provider.GetServices<ICanResolveReadModelForCommand>().OfType<ChronicleReadModelForCommandResolver>().Single();
    }

    void Because() => _resolved = _resolver.ReadModelTypes.ToDictionary(type => type, _scope.ServiceProvider.GetService);

    void Destroy()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    [Fact] void should_claim_interface_base_and_explicit_event_models() => _resolver.ReadModelTypes.ShouldContainOnly(typeof(InterfaceBalance), typeof(BaseBalance), typeof(ExplicitBalance));
    [Fact] void should_claim_declared_ownership() => _resolver.Ownership.ShouldEqual(ReadModelForCommandOwnership.Declared);
    [Fact] void should_not_claim_a_nominated_plain_reducer() => _resolved.ContainsKey(typeof(NominatedReducer)).ShouldBeFalse();
    [Fact] void should_inject_released_interface_event_state() => _resolved[typeof(InterfaceBalance)].ShouldBeSame(_interfaceReleased);
    [Fact] void should_inject_released_base_event_state() => _resolved[typeof(BaseBalance)].ShouldBeSame(_baseReleased);
    [Fact] void should_inject_released_explicit_event_state() => _resolved[typeof(ExplicitBalance)].ShouldBeSame(_explicitReleased);
    [Fact] void should_read_interface_event_state_through_chronicle() => _readModels.Received(1).GetInstanceById(Arg.Is<Type>(_ => _ == typeof(InterfaceBalance)), _id);
    [Fact] void should_read_base_event_state_through_chronicle() => _readModels.Received(1).GetInstanceById(Arg.Is<Type>(_ => _ == typeof(BaseBalance)), _id);
    [Fact] void should_read_explicit_event_state_through_chronicle() => _readModels.Received(1).GetInstanceById(Arg.Is<Type>(_ => _ == typeof(ExplicitBalance)), _id);
    [Fact] void should_release_every_injected_model() => _readModels.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IReadModels.Release)).ShouldEqual(3);

    void Configure<T>(T stored, T released)
        where T : class
    {
        _readModels.GetInstanceById(Arg.Is<Type>(_ => _ == typeof(T)), _id).Returns(Task.FromResult<object>(stored));
        _readModels.Release(stored).Returns(Task.FromResult(released));
    }

    interface IAccountEvent;
    record UnmarkedAccountEvent;
    record ExplicitAccountChanged : IAccountEvent;
    record DerivedAccountChanged : UnmarkedAccountEvent;

    record InterfaceBalance(int Amount)
    {
        public static InterfaceBalance? Reduce(IAccountEvent @event, InterfaceBalance? current) => current;
    }

    record BaseBalance(int Amount)
    {
        public static BaseBalance? Reduce(UnmarkedAccountEvent @event, BaseBalance? current) => current;
    }

    record ExplicitBalance(int Amount)
    {
        public static ExplicitBalance? Reduce(ExplicitAccountChanged @event, ExplicitBalance? current) => current;
    }

    class NominatedReducer
    {
        public Task<ExplicitBalance> Reduce(ExplicitAccountChanged @event, ExplicitBalance? current) => Task.FromResult(new ExplicitBalance(1));
    }
}
