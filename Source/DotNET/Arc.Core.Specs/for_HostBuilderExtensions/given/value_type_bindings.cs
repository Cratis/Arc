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

    public readonly record struct Value : IValue
    {
        public Value() => Number = 42;

        public int Number { get; }
    }

    public class Constructible;

    sealed class BindingsWithValueTypes : ICanProvideAssembliesForDiscovery, ICanProvideConventionsForDependencyInjection
    {
        public IEnumerable<Assembly> Assemblies => [];

        public IEnumerable<Type> DefinedTypes => [typeof(Policy), typeof(Value), typeof(IValue), typeof(Constructible)];

        public IEnumerable<ConventionServiceBinding> ConventionServiceBindings =>
        [
            new(typeof(IValue), typeof(Value), ServiceLifetime.Transient),
            new(typeof(IComparable), typeof(Policy), ServiceLifetime.Transient)
        ];

        public IEnumerable<ConventionSelfBinding> SelfBindings =>
        [
            new(typeof(Policy), ServiceLifetime.Transient),
            new(typeof(Value), ServiceLifetime.Transient),
            new(typeof(Constructible), ServiceLifetime.Transient)
        ];

        public void Initialize()
        {
        }
    }
}
