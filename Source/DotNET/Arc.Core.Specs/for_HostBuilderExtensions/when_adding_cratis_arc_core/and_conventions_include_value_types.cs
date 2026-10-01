// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_HostBuilderExtensions.when_adding_cratis_arc_core;

[Collection("MutatesTypeDiscovery")]
public class and_conventions_include_value_types : given.value_type_bindings
{
    void Because()
    {
        _services.AddCratisArcCore();
        _provider = _services.BuildServiceProvider();
    }

    [Fact] void should_not_self_bind_enums() => _services.Where(_ => _.ServiceType == typeof(Policy)).ShouldBeEmpty();
    [Fact] void should_not_bind_interfaces_to_enums() => _services.Where(_ => _.ServiceType == typeof(IComparable)).ShouldBeEmpty();
    [Fact] void should_resolve_self_bound_structs_with_a_public_constructor() => _provider.GetRequiredService<Value>().Number.ShouldEqual(42);
    [Fact] void should_resolve_convention_bound_structs_with_a_public_constructor() => ((Value)_provider.GetRequiredService<IValue>()).Number.ShouldEqual(42);
    [Fact] void should_still_resolve_concrete_classes() => _provider.GetRequiredService<Constructible>().ShouldBeOfExactType<Constructible>();
}
