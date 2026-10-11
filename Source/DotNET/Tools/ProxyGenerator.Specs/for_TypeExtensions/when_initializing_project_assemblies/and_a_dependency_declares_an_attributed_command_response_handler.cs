// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_initializing_project_assemblies;

public class and_a_dependency_declares_an_attributed_command_response_handler : Specification
{
    bool _initialized;
    bool _eventHandled;
    bool _arrayHandled;
    bool _rectangularArrayHandled;
    bool _nonVectorArrayHandled;
    bool _structArrayHandled;
    bool _structEnumerableHandled;
    bool _scalarOnlyHandled;
    bool _scalarOnlyArrayHandled;
    bool _enumerableOnlyScalarHandled;
    bool _enumerableOnlyCollectionHandled;
    bool _nonInheritedDirectHandled;
    bool _nonInheritedDerivedHandled;
    bool _defaultInheritedHandled;
    bool _inheritedEventHandled;
    bool _objectCollectionHandled;
    bool _counterfeitMarkerHandled;
    bool _counterfeitEnumerableMarkerHandled;
    bool _counterfeitAttributeHandled;
    bool _secondInitialized;
    bool _dependencyTypesCleared;
    List<string> _errors;

    void Establish() => _errors = [];

    void Because()
    {
        _initialized = TypeExtensions.TryInitializeProjectAssemblies(GetType().Assembly.Location, _ => { }, _errors.Add);
        using (TypeExtensions.OwnProjectAssemblies())
        {
            var eventType = FindMetadataType(typeof(ProjectRegistered).FullName!);
            _eventHandled = eventType.IsServerHandledCommandResponseValue();
            _arrayHandled = eventType.MakeArrayType().IsServerHandledCommandResponseValue();
            _rectangularArrayHandled = eventType.MakeArrayType(2).IsServerHandledCommandResponseValue();
            _nonVectorArrayHandled = eventType.MakeArrayType(1).IsServerHandledCommandResponseValue();
            _structArrayHandled = FindMetadataType(typeof(ConsumedStruct).FullName!).MakeArrayType().IsServerHandledCommandResponseValue();
            _structEnumerableHandled = FindMetadataType(typeof(for_MethodInfoExtensions.when_getting_a_response_model.given.command_methods).FullName!)
                .GetMethod(nameof(for_MethodInfoExtensions.when_getting_a_response_model.given.command_methods.ReturnsStructEventEnumerable))!
                .ReturnType.IsServerHandledCommandResponseValue();
            var scalarOnlyType = FindMetadataType(typeof(ScalarOnlyValue).FullName!);
            _scalarOnlyHandled = scalarOnlyType.IsServerHandledCommandResponseValue();
            _scalarOnlyArrayHandled = scalarOnlyType.MakeArrayType().IsServerHandledCommandResponseValue();
            var enumerableOnlyType = FindMetadataType(typeof(EnumerableOnlyValue).FullName!);
            _enumerableOnlyScalarHandled = enumerableOnlyType.IsServerHandledCommandResponseValue();
            _enumerableOnlyCollectionHandled = enumerableOnlyType.MakeArrayType().IsServerHandledCommandResponseValue();
            _nonInheritedDirectHandled = FindMetadataType(typeof(NonInheritedEvent).FullName!).IsServerHandledCommandResponseValue();
            _nonInheritedDerivedHandled = FindMetadataType(typeof(DerivedNonInheritedEvent).FullName!).IsServerHandledCommandResponseValue();
            _defaultInheritedHandled = FindMetadataType(typeof(DerivedDefaultInheritedEvent).FullName!).IsServerHandledCommandResponseValue();
            _inheritedEventHandled = FindMetadataType(typeof(InheritedProjectRegistered).FullName!).IsServerHandledCommandResponseValue();
            _objectCollectionHandled = FindMetadataType(typeof(for_MethodInfoExtensions.when_getting_a_response_model.given.command_methods).FullName!)
                .GetMethod(nameof(for_MethodInfoExtensions.when_getting_a_response_model.given.command_methods.ReturnsObjectCollection))!
                .ReturnType.IsServerHandledCommandResponseValue();
            _counterfeitMarkerHandled = FindMetadataType(typeof(for_MethodInfoExtensions.when_getting_a_response_model.given.CounterfeitClaimedValue).FullName!).IsServerHandledCommandResponseValue();
            _counterfeitEnumerableMarkerHandled = FindMetadataType(typeof(for_MethodInfoExtensions.when_getting_a_response_model.given.CounterfeitClaimedValue).FullName!).MakeArrayType().IsServerHandledCommandResponseValue();
            _counterfeitAttributeHandled = FindMetadataType("Cratis.Arc.ProxyGenerator.Specs.FakeCommandResponseHandlerDependency.FakeAttributedValue").IsServerHandledCommandResponseValue();
        }

        _secondInitialized = TypeExtensions.TryInitializeProjectAssemblies(typeof(CommandResult).Assembly.Location, _ => { }, _errors.Add);
        using (TypeExtensions.OwnProjectAssemblies())
        {
            _dependencyTypesCleared = !TypeExtensions.Assemblies.SelectMany(_ => _.GetTypes()).Any(_ => _.FullName == typeof(ProjectRegistered).FullName);
        }
    }

    static Type FindMetadataType(string fullName) => TypeExtensions.Assemblies.SelectMany(_ => _.GetTypes()).First(_ => _.FullName == fullName);

    [Fact] void should_initialize_the_real_metadata_graph() => Assert.True(_initialized, string.Join(Environment.NewLine, _errors));
    [Fact] void should_consume_an_attributed_event() => _eventHandled.ShouldBeTrue();
    [Fact] void should_consume_a_typed_event_array() => _arrayHandled.ShouldBeTrue();
    [Fact] void should_preserve_a_rectangular_event_array() => _rectangularArrayHandled.ShouldBeFalse();
    [Fact] void should_preserve_a_non_vector_event_array() => _nonVectorArrayHandled.ShouldBeFalse();
    [Fact] void should_preserve_an_attributed_struct_array() => _structArrayHandled.ShouldBeFalse();
    [Fact] void should_preserve_an_attributed_struct_enumerable() => _structEnumerableHandled.ShouldBeFalse();
    [Fact] void should_consume_a_scalar_with_only_scalar_handling_declared() => _scalarOnlyHandled.ShouldBeTrue();
    [Fact] void should_preserve_an_array_with_only_scalar_handling_declared() => _scalarOnlyArrayHandled.ShouldBeFalse();
    [Fact] void should_preserve_a_scalar_with_only_enumerable_handling_declared() => _enumerableOnlyScalarHandled.ShouldBeFalse();
    [Fact] void should_consume_a_collection_with_only_enumerable_handling_declared() => _enumerableOnlyCollectionHandled.ShouldBeTrue();
    [Fact] void should_consume_a_direct_attribute_without_inheritance() => _nonInheritedDirectHandled.ShouldBeTrue();
    [Fact] void should_preserve_a_derived_value_without_attribute_inheritance() => _nonInheritedDerivedHandled.ShouldBeFalse();
    [Fact] void should_consume_an_attribute_with_default_inheritance() => _defaultInheritedHandled.ShouldBeTrue();
    [Fact] void should_consume_an_event_with_an_inherited_attribute() => _inheritedEventHandled.ShouldBeTrue();
    [Fact] void should_preserve_an_object_collection() => _objectCollectionHandled.ShouldBeFalse();
    [Fact] void should_ignore_a_counterfeit_marker_with_a_genuine_runtime_handler() => _counterfeitMarkerHandled.ShouldBeFalse();
    [Fact] void should_ignore_a_counterfeit_enumerable_marker_with_a_genuine_runtime_handler() => _counterfeitEnumerableMarkerHandled.ShouldBeFalse();
    [Fact] void should_ignore_an_attribute_from_a_different_assembly() => _counterfeitAttributeHandled.ShouldBeFalse();
    [Fact] void should_initialize_a_second_project_graph() => Assert.True(_secondInitialized, string.Join(Environment.NewLine, _errors));
    [Fact] void should_clear_the_previous_dependency_types() => _dependencyTypesCleared.ShouldBeTrue();
}
