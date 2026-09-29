// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_ModelGraphWalkerGenerator;

public class when_generating_model_graph_walkers : Specification
{
    const string Models = """
        using System;
        using System.Collections.Generic;
        using System.Diagnostics.CodeAnalysis;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Concepts;
        #pragma warning disable CRATIS001, CS0618
        namespace Models;
        public record Item(string Name);
        public record Wrapper<T>(T Value, int Count);
        public record Identifier(Guid Value) : ConceptAs<Guid>(Value);
        public record struct Point(int X, int Y);
        public enum Kind { One }
        public class Base { public string First { get; set; } = "first"; public virtual string Overridden { get; set; } = "base"; public int OtherType { get; set; } public string SameType { get; set; } = "base"; }
        public class Derived : Base
        {
            public string Own { get; set; } = "own";
            public override string Overridden { get; set; } = "derived";
            public new string OtherType { get; set; } = "other";
            public new string SameType { get; set; } = "same";
            public static string Static { get; set; } = "static";
            public string WriteOnly { set { } }
            public string this[int index] => string.Empty;
            internal string Internal { get; set; } = "internal";
        }
        public class PrivatelyRead { public string Value { private get; set; } = "v"; public Reached Reached { get; set; } = new("through a type without a walker"); }
        public record Reached(string Name);
        public class Parent { public Child? Child { get; set; } }
        public class Child { public Parent? Parent { get; set; } }
        public abstract record Shape(string Label);
        public record Circle(string Label, Item Center) : Shape(Label);
        public interface IThing { Item Thing { get; } }
        public record OnlyInObject(string Name);
        public record Legacy { [Obsolete] public string Old { get; set; } = string.Empty; }
        [Obsolete] public record Retired;
        public record UsesRetired(Retired? Retired);
        public record Preview { [Experimental("CRATIS001")] public string Next { get; set; } = string.Empty; }
        public class FromReference : Shared.ReferenceBase { public string Own { get; set; } = string.Empty; }
        public class FromLibrary : Library.LibraryBase { public string Own { get; set; } = string.Empty; }
        public record Keyed(Dictionary<string, KeyedValue> ByKey, List<Point> Points, Element[] Elements, Nested? Maybe, IEnumerable<Enumerated> Enumerated);
        public record KeyedValue(string Name);
        public record Element(string Name);
        public record struct Nested(int Value);
        public record Enumerated(string Name);
        public class Node { public List<Node> Children { get; } = new(); public Node? Next { get; set; } }
        public record Recursive<T>(Recursive<List<T>>? Deeper, T Value);
        public record Tupled((Item Left, Kind Right) Pair);
        public class ByRef { int _value; public ref int Value => ref _value; }
        public class Outer { private record Secret(string Value); [Command] private record Hush(Secret Secret) { public void Handle() { } } }
        [Command] public record DoIt(Derived Derived, PrivatelyRead PrivatelyRead, Parent Parent, Shape Shape, IThing? Thing, object Payload, Legacy Legacy,
            UsesRetired UsesRetired, Preview Preview, FromReference FromReference, FromLibrary FromLibrary, Keyed Keyed, Node Node, Wrapper<Item> Wrapper,
            Identifier Id, Recursive<int>? Recursive, Tupled Tupled, ByRef ByRef, dynamic Dynamic) { public void Handle() { } }
        [ReadModel] public record Order(string Id)
        {
            public static IEnumerable<Order> ByFilter(Filter filter) => [];
            public static IEnumerable<Order> Search(string term) => [];
        }
        public record Filter(string Term, FilterOwner Owner);
        public record FilterOwner(string Name);
        public record OrderSearchParameters(string Term);
        public record NotReachable(string Name);
        """;

    string _source;
    Diagnostic[] _diagnostics;

    void Because()
    {
        var referenceLibrary = ModelGraphWalkerCompilation.Library("ReferenceLibrary", "namespace Shared; public class ReferenceBase { public string Inherited { get; set; } = string.Empty; }", true);
        var implementationLibrary = ModelGraphWalkerCompilation.Library("ImplementationLibrary", "namespace Library; public class LibraryBase { public string Inherited { get; set; } = string.Empty; }", false);
        (_source, _diagnostics, _) = ModelGraphWalkerCompilation.Compile(
            "ModelGraphWalkerGeneratorSpec",
            Models,
            true,
            referenceLibrary,
            implementationLibrary);
    }

    [Fact] void should_emit_compilable_walkers_without_warnings() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_register_at_assembly_load() => _source.ShouldContain("ModuleInitializer");
    [Fact] void should_register_the_command() => Registers("global::Models.DoIt").ShouldBeTrue();
    [Fact] void should_register_nested_types() => Registers("global::Models.Item").ShouldBeTrue();
    [Fact] void should_register_each_type_once() => Occurrences("Register(typeof(global::Models.Item)").ShouldEqual(1);
    [Fact] void should_register_constructed_generic_types() => Registers("global::Models.Wrapper<global::Models.Item>").ShouldBeTrue();
    [Fact] void should_register_concepts_with_their_inherited_value() => _source.ShouldContain("((global::Cratis.Concepts.ConceptAs<global::System.Guid>)instance).@Value");
    [Fact] void should_register_structs() => Registers("global::Models.Point").ShouldBeTrue();
    [Fact] void should_register_nullable_value_types_by_their_value() => Registers("global::Models.Nested").ShouldBeTrue();
    [Fact] void should_register_dictionary_values() => Registers("global::Models.KeyedValue").ShouldBeTrue();
    [Fact] void should_register_array_elements() => Registers("global::Models.Element").ShouldBeTrue();
    [Fact] void should_register_enumerable_elements() => Registers("global::Models.Enumerated").ShouldBeTrue();
    [Fact] void should_not_register_collections() => _source.ShouldNotContain("Register(typeof(global::System.Collections");
    [Fact] void should_not_register_types_declared_elsewhere() => _source.ShouldNotContain("Register(typeof(global::System.Collections.Generic.KeyValuePair");
    [Fact] void should_not_register_enums() => Registers("global::Models.Kind").ShouldBeFalse();
    [Fact] void should_register_both_sides_of_a_cycle() => (Registers("global::Models.Parent") && Registers("global::Models.Child")).ShouldBeTrue();
    [Fact] void should_register_self_referencing_types() => Registers("global::Models.Node").ShouldBeTrue();
    [Fact] void should_bound_self_expanding_generic_types() => Registers("global::Models.Recursive<int>").ShouldBeTrue();
    [Fact] void should_register_tuple_elements_through_their_type_arguments() => Registers("global::Models.Tupled").ShouldBeTrue();
    [Fact] void should_walk_inherited_members_after_own_members() => _source.IndexOf("((global::Models.Derived)instance).@Own", StringComparison.Ordinal).ShouldBeLessThan(_source.IndexOf("((global::Models.Base)instance).@First", StringComparison.Ordinal));
    [Fact] void should_read_an_override_once_through_the_derived_type() => Occurrences("instance).@Overridden").ShouldEqual(1);
    [Fact] void should_read_a_member_hidden_by_another_type_through_its_base() => _source.ShouldContain("((global::Models.Base)instance).@OtherType");
    [Fact] void should_not_read_a_member_hidden_by_the_same_type() => _source.ShouldNotContain("((global::Models.Base)instance).@SameType");
    [Fact] void should_not_read_static_members() => _source.ShouldNotContain("@Static");
    [Fact] void should_not_read_write_only_members() => _source.ShouldNotContain("@WriteOnly");
    [Fact] void should_not_read_non_public_members() => _source.ShouldNotContain("@Internal");
    [Fact] void should_not_read_indexers() => _source.ShouldNotContain("@this");
    [Fact] void should_describe_dynamic_members_as_object() => _source.ShouldContain("new global::Cratis.Arc.Validation.ModelGraphMember(\"Dynamic\", typeof(object)");
    [Fact] void should_not_register_types_with_a_member_it_cannot_read() => Registers("global::Models.PrivatelyRead").ShouldBeFalse();
    [Fact] void should_still_follow_types_without_a_walker() => Registers("global::Models.Reached").ShouldBeTrue();
    [Fact] void should_not_register_types_with_members_returned_by_reference() => Registers("global::Models.ByRef").ShouldBeFalse();
    [Fact] void should_not_register_abstract_types() => Registers("global::Models.Shape").ShouldBeFalse();
    [Fact] void should_not_register_subtypes_only_known_at_runtime() => Registers("global::Models.Circle").ShouldBeFalse();
    [Fact] void should_not_register_interfaces() => Registers("global::Models.IThing").ShouldBeFalse();
    [Fact] void should_not_register_types_only_held_by_object() => Registers("global::Models.OnlyInObject").ShouldBeFalse();
    [Fact] void should_not_register_types_with_obsolete_members() => Registers("global::Models.Legacy").ShouldBeFalse();
    [Fact] void should_not_register_types_with_members_of_obsolete_types() => Registers("global::Models.UsesRetired").ShouldBeFalse();
    [Fact] void should_not_register_types_with_experimental_members() => Registers("global::Models.Preview").ShouldBeFalse();
    [Fact] void should_not_register_types_inheriting_from_reference_assemblies() => Registers("global::Models.FromReference").ShouldBeFalse();
    [Fact] void should_register_types_inheriting_from_implementation_assemblies() => _source.ShouldContain("((global::Library.LibraryBase)instance).@Inherited");
    [Fact] void should_not_register_inaccessible_types() => _source.ShouldNotContain("Secret");
    [Fact] void should_register_query_parameters() => Registers("global::Models.Filter").ShouldBeTrue();
    [Fact] void should_register_types_nested_in_query_parameters() => Registers("global::Models.FilterOwner").ShouldBeTrue();
    [Fact] void should_register_query_arguments_models() => Registers("global::Models.OrderSearchParameters").ShouldBeTrue();
    [Fact] void should_not_register_read_models() => Registers("global::Models.Order").ShouldBeFalse();
    [Fact] void should_not_register_unreachable_types() => Registers("global::Models.NotReachable").ShouldBeFalse();

    bool Registers(string type) => _source.Contains($"Register(typeof({type}),", StringComparison.Ordinal);

    int Occurrences(string text) => _source.Split(text).Length - 1;
}
