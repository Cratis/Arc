// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Generators.Specs.for_ModelGraphWalkerGenerator;

/// <summary>
/// The same models are compiled twice, once with the generator and once without, so one copy is walked through the
/// generated members and the other through reflection. Both traversals must reach the same nodes, in the same order,
/// under the same member paths.
/// </summary>
public class when_walking_generated_members : Specification
{
    const string Models = """
        using System;
        using System.Collections.Generic;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Concepts;
        namespace Walked;
        public record Item(string Name);
        public record Wrapper<T>(T Value, int Count);
        public record Identifier(Guid Value) : ConceptAs<Guid>(Value);
        public record struct Point(int X, int Y);
        public class Base { public string First { get; set; } = "first"; public virtual string Overridden { get; set; } = "base"; public string Last { get; set; } = "last"; public int OtherType { get; set; } = 42; public string SameType { get; set; } = "base same"; }
        public class Derived : Base
        {
            public string Own { get; set; } = "own";
            public override string Overridden { get; set; } = "derived";
            public new string OtherType { get; set; } = "other";
            public new string SameType { get; set; } = "same";
            public static string Static { get; set; } = "static";
            public string WriteOnly { set { } }
            public string this[int index] => index.ToString();
            internal string Internal { get; set; } = "internal";
        }
        public record General(string Name);
        public record Special(string Name, string Extra) : General(Name);
        public class Parent { public Child? Child { get; set; } }
        public class Child { public Parent? Parent { get; set; } }
        public record Root(Derived Derived, List<Item> List, Item[] Array, Dictionary<string, Item> ByKey, object Payload, General General, string? Missing,
            int? Count, int? NoCount, Point Point, Point? MaybePoint, Item First, Item Second, Wrapper<Item> Wrapper, Identifier Id, Parent Parent);
        [Command] public record Validate(Root Root) { public void Handle() { } }
        public static class Samples
        {
            public static Validate Create()
            {
                var shared = new Item("shared");
                var parent = new Parent();
                parent.Child = new Child { Parent = parent };
                return new(new Root(
                    new Derived(),
                    [new Item("a"), null!, new Item("b")],
                    [new Item("c")],
                    new Dictionary<string, Item> { ["k"] = new Item("d") },
                    new Item("held"),
                    new Special("general", "special"),
                    null,
                    5,
                    null,
                    new Point(1, 2),
                    new Point(3, 4),
                    shared,
                    shared,
                    new Wrapper<Item>(new Item("wrapped"), 3),
                    new Identifier(Guid.Empty),
                    parent));
            }
        }
        """;

    Assembly _generated;
    Assembly _reflected;
    string _generatedTraversal;
    string _reflectedTraversal;

    void Establish()
    {
        var (source, diagnostics, output) = ModelGraphWalkerCompilation.Compile("GeneratedWalkers", Models, true);
        diagnostics.ShouldBeEmpty();
        source.ShouldNotBeEmpty();
        _generated = ModelGraphWalkerCompilation.Load(output);
        _reflected = ModelGraphWalkerCompilation.Load(ModelGraphWalkerCompilation.Compile("ReflectedWalkers", Models, false).Output);
    }

    void Because()
    {
        _generatedTraversal = ModelGraphWalkerCompilation.Walk(_generated);
        _reflectedTraversal = ModelGraphWalkerCompilation.Walk(_reflected);
    }

    [Fact] void should_walk_the_same_graph_as_reflection() => _generatedTraversal.ShouldEqual(_reflectedTraversal);
    [Fact] void should_walk_the_whole_graph() => _generatedTraversal.Split(" | ").Length.ShouldEqual(45);
    [Fact] void should_walk_generated_members_for_registered_types() => GeneratedMembersOf("Walked.Root")[0].Read.Method.DeclaringType!.Assembly.ShouldEqual(_generated);
    [Fact] void should_not_register_types_without_generated_code() => ModelGraphWalkers.TryGet(_reflected.GetType("Walked.Root")!, out _).ShouldBeFalse();
    [Fact] void should_fall_back_to_reflection_for_types_without_a_walker() => ModelGraphWalkers.TryGet(_generated.GetType("Walked.Special")!, out _).ShouldBeFalse();
    [Fact] void should_fall_back_to_reflection_for_types_overriding_or_hiding_members() => ModelGraphWalkers.TryGet(_generated.GetType("Walked.Derived")!, out _).ShouldBeFalse();
    [Fact] void should_fall_back_to_reflection_for_types_inheriting_from_other_assemblies() => ModelGraphWalkers.TryGet(_generated.GetType("Walked.Identifier")!, out _).ShouldBeFalse();
    [Fact] void should_generate_the_members_reflection_finds_for_every_registered_type() =>
        ModelGraphWalkerCompilation.Describe(_generated, ModelGraphValidator.GetWalkableProperties, ConstructedWrapper())
            .ShouldEqual(ModelGraphWalkerCompilation.Describe(_generated, ModelGraphValidator.GetWalkablePropertiesThroughReflection, ConstructedWrapper()));
    [Fact] void should_describe_registered_constructed_generic_types() =>
        ModelGraphWalkerCompilation.Describe(_generated, ModelGraphValidator.GetWalkableProperties, ConstructedWrapper()).ShouldContain("Walked.Wrapper`1[Walked.Item]: value, count");

    Type ConstructedWrapper() => _generated.GetType("Walked.Wrapper`1")!.MakeGenericType(_generated.GetType("Walked.Item")!);

    ModelGraphMember[] GeneratedMembersOf(string type)
    {
        ModelGraphWalkers.TryGet(_generated.GetType(type)!, out var members).ShouldBeTrue();
        return members!;
    }
}
