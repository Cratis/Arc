// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_HostBuilderExtensions.given;

public class value_type_bindings : Specification
{
    protected IServiceCollection _services;
    protected ServiceProvider _provider;

    void Establish()
    {
        GeneratedTypeDiscoveryRegistry.Register(new BindingsWithValueTypes());
        _services = new ServiceCollection();
    }

    void Destroy() => _provider?.Dispose();

    public enum Policy
    {
        Default = 0,
        Strict = 1
    }

    public interface IValue;

    public readonly record struct Value : IValue;

    public interface IParameterlessValue;

    public readonly record struct ParameterlessValue : IParameterlessValue
    {
        public ParameterlessValue() => Number = 42;

        public int Number { get; }
    }

    public interface IClock
    {
        TimeProvider Provider { get; }
    }

    public readonly record struct Clock(TimeProvider Provider) : IClock;

    public readonly record struct SelfBoundClock(TimeProvider Provider);

    public class OpenGeneric<T>;

    public class Constructible;

    public abstract class Abstract;

    public delegate void Callback();

    sealed class BindingsWithValueTypes : ICanProvideAssembliesForDiscovery, ICanProvideConventionsForDependencyInjection
    {
        public IEnumerable<Assembly> Assemblies => [];

        public IEnumerable<Type> DefinedTypes => [typeof(Policy), typeof(Value), typeof(IValue), typeof(Constructible), typeof(Abstract), typeof(Callback), typeof(Clock), typeof(IClock), typeof(SelfBoundClock), typeof(OpenGeneric<>), typeof(ParameterlessValue), typeof(IParameterlessValue)];

        public IEnumerable<ConventionServiceBinding> ConventionServiceBindings =>
        [
            new(typeof(IValue), typeof(Value), ServiceLifetime.Transient),
            new(typeof(IClock), typeof(Clock), ServiceLifetime.Transient),
            new(typeof(IComparable), typeof(Policy), ServiceLifetime.Transient),
            new(typeof(IParameterlessValue), typeof(ParameterlessValue), ServiceLifetime.Transient)
        ];

        public IEnumerable<ConventionSelfBinding> SelfBindings =>
        [
            new(typeof(Policy), ServiceLifetime.Transient),
            new(typeof(Value), ServiceLifetime.Transient),
            new(typeof(Abstract), ServiceLifetime.Transient),
            new(typeof(Callback), ServiceLifetime.Transient),
            new(typeof(IValue), ServiceLifetime.Transient),
            new(typeof(Clock), ServiceLifetime.Transient),
            new(typeof(SelfBoundClock), ServiceLifetime.Transient),
            new(typeof(OpenGeneric<>), ServiceLifetime.Transient),
            new(typeof(ParameterlessValue), ServiceLifetime.Transient),
            new(typeof(Constructible), ServiceLifetime.Transient)
        ];

        public void Initialize()
        {
        }
    }
}
