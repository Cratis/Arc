// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation.for_DiscoverableValidators;

public class when_resolving_with_read_protection
{
    [Theory]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Singleton)]
    public void should_refuse_a_reusable_registered_validator(ServiceLifetime lifetime)
    {
        var types = TypesForValidator();
        var services = new ServiceCollection().AddSingleton(new State());
        services.Add(new ServiceDescriptor(typeof(StateValidator), typeof(StateValidator), lifetime));
        services.ConstructDependencyFreeValidatorsThroughArc(types);
        using var provider = services.BuildServiceProvider();
        using var policy = CommandDecisionPolicy.Begin(typeof(GuardedCommand));
        var validators = new DiscoverableValidators(types);

        Catch.Exception(() => validators.TryGet(typeof(GuardedCommand), provider, out _))
            .ShouldBeOfExactType<DiscoverableValidatorRefusedInProtectedDecision>();
    }

    [Fact]
    public void should_construct_a_transient_dependency_taking_validator_in_the_invocation()
    {
        var types = TypesForValidator();
        var state = new State();
        var services = new ServiceCollection()
            .AddSingleton(Substitute.For<ICommandProtectedDecisionSupport>())
            .AddSingleton(state)
            .AddTransient<StateValidator>();
        services.ConstructDependencyFreeValidatorsThroughArc(types);
        using var provider = services.BuildServiceProvider();
        using var policy = CommandDecisionPolicy.Begin(typeof(GuardedCommand));
        var validators = new DiscoverableValidators(types);

        validators.TryGet(typeof(GuardedCommand), provider, out var first).ShouldBeTrue();
        validators.TryGet(typeof(GuardedCommand), provider, out var second).ShouldBeTrue();
        ((StateValidator)first).State.ShouldBeSame(state);
        ReferenceEquals(first, second).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_refuse_factory_and_instance_registrations(bool instance)
    {
        var types = TypesForValidator();
        var services = new ServiceCollection();
        if (instance)
        {
            services.AddSingleton(new StateValidator(new State()));
        }
        else
        {
            services.AddTransient(_ => new StateValidator(new State()));
        }
        services.ConstructDependencyFreeValidatorsThroughArc(types);
        using var provider = services.BuildServiceProvider();
        using var policy = CommandDecisionPolicy.Begin(typeof(GuardedCommand));

        Catch.Exception(() => new DiscoverableValidators(types).TryGet(typeof(GuardedCommand), provider, out _))
            .ShouldBeOfExactType<DiscoverableValidatorRefusedInProtectedDecision>();
    }

    [Fact]
    public void should_not_accept_a_validator_from_an_earlier_invocation()
    {
        var types = TypesForValidator();
        var services = new ServiceCollection()
            .AddSingleton(Substitute.For<ICommandProtectedDecisionSupport>())
            .AddSingleton(new State())
            .AddTransient<StateValidator>();
        services.ConstructDependencyFreeValidatorsThroughArc(types);
        using var provider = services.BuildServiceProvider();
        StateValidator previous;
        using (CommandDecisionPolicy.Begin(typeof(GuardedCommand)))
        {
            previous = provider.GetRequiredService<StateValidator>();
        }
        using var offered = new ServiceCollection().AddSingleton(previous).BuildServiceProvider();
        using var policy = CommandDecisionPolicy.Begin(typeof(GuardedCommand));

        Catch.Exception(() => new DiscoverableValidators(types).TryGet(typeof(GuardedCommand), offered, out _))
            .ShouldBeOfExactType<DiscoverableValidatorRefusedInProtectedDecision>();
    }

    [Fact]
    public void should_construct_a_convention_transient_validator_of_a_child_model_in_the_invocation()
    {
        var types = TypesForValidator();
        var state = new State();
        var services = new ServiceCollection()
            .AddSingleton(Substitute.For<ICommandProtectedDecisionSupport>())
            .AddSingleton(state)
            .AddTransient<StateValidator>()
            .AddTransient<ChildValidator>();
        services.ConstructDependencyFreeValidatorsThroughArc(types);
        using var provider = services.BuildServiceProvider();
        using var policy = CommandDecisionPolicy.Begin(typeof(GuardedCommand));
        var validators = new DiscoverableValidators(types);

        validators.TryGet(typeof(Child), provider, out var first).ShouldBeTrue();
        validators.TryGet(typeof(Child), provider, out var second).ShouldBeTrue();
        ((ChildValidator)first).State.ShouldBeSame(state);
        ReferenceEquals(first, second).ShouldBeFalse();
    }

    [Fact]
    public void should_refuse_a_reusable_registration_of_a_child_model_validator()
    {
        var types = TypesForValidator();
        var services = new ServiceCollection()
            .AddSingleton(new State())
            .AddScoped<ChildValidator>();
        services.ConstructDependencyFreeValidatorsThroughArc(types);
        using var provider = services.BuildServiceProvider();
        using var policy = CommandDecisionPolicy.Begin(typeof(GuardedCommand));

        Catch.Exception(() => new DiscoverableValidators(types).TryGet(typeof(Child), provider, out _))
            .ShouldBeOfExactType<DiscoverableValidatorRefusedInProtectedDecision>();
    }

    [Fact]
    public void should_not_construct_a_dependency_taking_convention_validator_through_arc_outside_read_protection()
    {
        var types = TypesForValidator();
        var services = new ServiceCollection()
            .AddSingleton(new State())
            .AddTransient<StateValidator>();
        services.ConstructDependencyFreeValidatorsThroughArc(types);

        services.Single(_ => _.ServiceType == typeof(StateValidator)).ImplementationType.ShouldEqual(typeof(StateValidator));
    }

    static ITypes TypesForValidator()
    {
        var types = Substitute.For<ITypes>();
        types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(StateValidator), typeof(ChildValidator)]);
        return types;
    }

    [ReadProtection(ReadProtectionMode.On)]
    public record GuardedCommand;
    public record Child;
    public class State;
    public class ChildValidator(State state) : AbstractValidator<Child>, IDiscoverableValidator<Child>
    {
        public State State { get; } = state;
    }
    public class StateValidator(State state) : CommandValidator<GuardedCommand>
    {
        public State State { get; } = state;
    }
}
