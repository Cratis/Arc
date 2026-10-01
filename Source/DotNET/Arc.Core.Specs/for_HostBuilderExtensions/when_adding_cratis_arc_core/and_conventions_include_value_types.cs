// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Metrics;
using Cratis.Traces;
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
    [Fact] void should_resolve_a_closed_self_bound_generic() => _provider.GetRequiredService<OpenGeneric<Constructible>>().ShouldBeOfExactType<OpenGeneric<Constructible>>();
    [Fact] void should_resolve_an_unkeyed_meter_for_an_application_type() => _provider.GetRequiredService<IMeter<Constructible>>().ShouldNotBeNull();
    [Fact] void should_resolve_an_unkeyed_activity_source_for_an_application_type() => _provider.GetRequiredService<IActivitySource<Constructible>>().ShouldNotBeNull();
    [Fact] void should_not_bind_interfaces_to_enums() => _services.Where(_ => _.ServiceType == typeof(IComparable)).ShouldBeEmpty();
    [Fact] void should_resolve_a_self_bound_struct_with_a_parameterless_constructor() => _provider.GetRequiredService<ParameterlessValue>().Number.ShouldEqual(42);
    [Fact] void should_resolve_a_convention_bound_struct_with_a_parameterless_constructor() => ((ParameterlessValue)_provider.GetRequiredService<IParameterlessValue>()).Number.ShouldEqual(42);
    [Fact] void should_resolve_a_self_bound_struct() => _provider.GetRequiredService<SelfBoundClock>().Provider.ShouldEqual(TimeProvider.System);
    [Fact] void should_resolve_a_self_bound_record_struct() => _provider.GetRequiredService<Clock>().Provider.ShouldEqual(TimeProvider.System);
    [Fact] void should_resolve_a_convention_bound_record_struct() => _provider.GetRequiredService<IClock>().Provider.ShouldEqual(TimeProvider.System);
    [Fact] void should_still_resolve_concrete_classes() => _provider.GetRequiredService<Constructible>().ShouldBeOfExactType<Constructible>();
}
