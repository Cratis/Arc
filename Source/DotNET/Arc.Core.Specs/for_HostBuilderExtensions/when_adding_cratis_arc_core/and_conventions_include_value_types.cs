// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_HostBuilderExtensions.when_adding_cratis_arc_core;

[Collection("MutatesTypeDiscovery")]
public class and_conventions_include_value_types : given.value_type_bindings
{
    void Establish() => _services.AddSingleton(TimeProvider.System);

    void Because()
    {
        _services.AddCratisArcCore();
        _provider = _services.BuildServiceProvider();
    }

    [Fact] void should_not_self_bind_enums() => _services.Where(_ => _.ServiceType == typeof(Policy)).ShouldBeEmpty();
    [Fact] void should_not_self_bind_structs_without_public_constructors() => _services.Where(_ => _.ServiceType == typeof(Value)).ShouldBeEmpty();
    [Fact] void should_not_self_bind_abstract_classes() => _services.Where(_ => _.ServiceType == typeof(Abstract)).ShouldBeEmpty();
    [Fact] void should_not_self_bind_delegates() => _services.Where(_ => _.ServiceType == typeof(Callback)).ShouldBeEmpty();
    [Fact] void should_not_bind_interfaces_to_structs_without_public_constructors() => _services.Where(_ => _.ServiceType == typeof(IValue)).ShouldBeEmpty();
    [Fact] void should_not_self_bind_interfaces() => _services.Where(_ => _.ServiceType == typeof(IValue)).ShouldBeEmpty();
    [Fact] void should_not_self_bind_open_generics() => _services.Where(_ => _.ServiceType == typeof(OpenGeneric<>)).ShouldBeEmpty();
    [Fact] void should_resolve_a_self_bound_struct() => _provider.GetRequiredService<SelfBoundClock>().Provider.ShouldEqual(TimeProvider.System);
    [Fact] void should_resolve_a_self_bound_record_struct() => _provider.GetRequiredService<Clock>().Provider.ShouldEqual(TimeProvider.System);
    [Fact] void should_resolve_a_convention_bound_record_struct() => _provider.GetRequiredService<IClock>().Provider.ShouldEqual(TimeProvider.System);
    [Fact] void should_still_resolve_concrete_classes() => _provider.GetRequiredService<Constructible>().ShouldBeOfExactType<Constructible>();
}
