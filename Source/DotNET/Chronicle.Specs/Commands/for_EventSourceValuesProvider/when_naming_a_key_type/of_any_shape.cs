// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_EventSourceValuesProvider.when_naming_a_key_type;

/// <summary>
/// Naming the key type only serves telemetry, so it must give a name for every shape of type rather than fail the command.
/// </summary>
public class of_any_shape : Specification
{
    string _plain;
    string _generic;
    string _nestedInGeneric;
    string _genericParameter;
    string _openGeneric;
    string _array;
    string _arrayOfGeneric;
    string _nullable;
    string _byReference;
    string _pointer;
    string _genericOfGenerics;
    Exception? _error;

    void Because() => _error = Catch.Exception(Name);

    void Name()
    {
        _plain = EventSourceValuesProvider.NameOf(typeof(Guid));
        _generic = EventSourceValuesProvider.NameOf(typeof(Key<Guid>));
        _nestedInGeneric = EventSourceValuesProvider.NameOf(typeof(Key<Guid>.Part));
        _genericParameter = EventSourceValuesProvider.NameOf(typeof(Key<>).GetGenericArguments()[0]);
        _openGeneric = EventSourceValuesProvider.NameOf(typeof(Key<>));
        _array = EventSourceValuesProvider.NameOf(typeof(Guid[]));
        _arrayOfGeneric = EventSourceValuesProvider.NameOf(typeof(Key<Guid>[]));
        _nullable = EventSourceValuesProvider.NameOf(typeof(Guid?));
        _byReference = EventSourceValuesProvider.NameOf(typeof(Guid).MakeByRefType());
        _pointer = EventSourceValuesProvider.NameOf(typeof(Guid).MakePointerType());
        _genericOfGenerics = EventSourceValuesProvider.NameOf(typeof(Key<Key<Guid>>));
    }

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_name_an_array() => _array.ShouldEqual("System.Guid[]");
    [Fact] void should_name_an_array_of_a_generic_type() => _arrayOfGeneric.ShouldNotBeNull();
    [Fact] void should_name_a_nullable_value_type() => _nullable.ShouldEqual("System.Nullable<System.Guid>");
    [Fact] void should_name_a_by_reference_type() => _byReference.ShouldNotBeNull();
    [Fact] void should_name_a_pointer_type() => _pointer.ShouldNotBeNull();
    [Fact] void should_name_a_generic_type_of_generic_types() => _genericOfGenerics.ShouldEqual($"{typeof(Key<>).Namespace}.of_any_shape+Key<{typeof(Key<>).Namespace}.of_any_shape+Key<System.Guid>>");
    [Fact] void should_name_a_plain_type_by_its_full_name() => _plain.ShouldEqual("System.Guid");
    [Fact] void should_name_a_generic_type_with_its_arguments() => _generic.ShouldEqual($"{typeof(Key<>).Namespace}.of_any_shape+Key<System.Guid>");
    [Fact] void should_name_a_type_nested_in_a_generic_type() => _nestedInGeneric.ShouldEqual($"{typeof(Key<>).Namespace}.of_any_shape+Key+Part<System.Guid>");
    [Fact] void should_name_a_generic_parameter() => _genericParameter.ShouldEqual("T");
    [Fact] void should_name_an_open_generic_type() => _openGeneric.ShouldEqual(typeof(Key<>).FullName);

    public record Key<T>(T Value)
    {
        public record Part(T Value);
    }
}
