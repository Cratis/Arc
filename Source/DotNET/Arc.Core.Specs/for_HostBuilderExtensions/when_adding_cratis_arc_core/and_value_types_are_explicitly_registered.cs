// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_HostBuilderExtensions.when_adding_cratis_arc_core;

[Collection("MutatesTypeDiscovery")]
public class and_value_types_are_explicitly_registered : given.value_type_bindings
{
    void Establish()
    {
        _services.AddSingleton(typeof(Policy), Policy.Strict);
        _services.AddSingleton(typeof(Value), _ => default(Value));
        _services.AddSingleton<IValue>(default(Value));
        _services.AddKeyedSingleton(typeof(Policy), "policy", Policy.Strict);
    }

    void Because()
    {
        _services.AddCratisArcCore();
        _provider = _services.BuildServiceProvider();
    }

    [Fact] void should_preserve_the_explicit_enum_value() => _provider.GetRequiredService<Policy>().ShouldEqual(Policy.Strict);
    [Fact] void should_preserve_the_explicit_struct_factory() => _provider.GetRequiredService<Value>().ShouldEqual(default);
    [Fact] void should_preserve_the_boxed_interface_implementation() => _provider.GetRequiredService<IValue>().ShouldBeOfExactType<Value>();
    [Fact] void should_preserve_the_keyed_enum_value() => _provider.GetRequiredKeyedService<Policy>("policy").ShouldEqual(Policy.Strict);
}
