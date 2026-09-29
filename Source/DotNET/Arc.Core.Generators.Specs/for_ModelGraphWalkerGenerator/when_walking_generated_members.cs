// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Validation;
using FluentValidation;

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
        _generatedTraversal = Walk(_generated);
        _reflectedTraversal = Walk(_reflected);
    }

    [Fact] void should_walk_the_same_graph_as_reflection() => _generatedTraversal.ShouldEqual(_reflectedTraversal);
    [Fact] void should_walk_the_whole_graph() => _generatedTraversal.Split(" | ").Length.ShouldEqual(45);
    [Fact] void should_walk_generated_members_for_registered_types() => GeneratedMembersOf("Walked.Root")[0].Read.Method.DeclaringType!.Assembly.ShouldEqual(_generated);
    [Fact] void should_not_register_types_without_generated_code() => ModelGraphWalkers.TryGet(_reflected.GetType("Walked.Root")!, out _).ShouldBeFalse();
    [Fact] void should_fall_back_to_reflection_for_types_without_a_walker() => ModelGraphWalkers.TryGet(_generated.GetType("Walked.Special")!, out _).ShouldBeFalse();
    [Fact] void should_generate_the_members_reflection_finds_for_every_registered_type() =>
        Describe(ModelGraphValidator.GetWalkableProperties).ShouldEqual(Describe(ModelGraphValidator.GetWalkablePropertiesThroughReflection));

    string Describe(Func<Type, WalkableMember[]> members) => string.Join(
        Environment.NewLine,
        _generated.GetTypes()
            .Where(type => ModelGraphWalkers.TryGet(type, out _))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .Select(type => $"{type}: {string.Join(", ", members(type).Select(member => member.Name))}"));

    static string Walk(Assembly assembly)
    {
        var visits = new List<string>();
        var discoverableValidators = Substitute.For<IDiscoverableValidators>();
        var validator = Substitute.For<IValidator>();
        discoverableValidators.TryGet(Arg.Any<Type>(), out Arg.Any<IValidator>())
            .Returns(x =>
            {
                x[1] = validator;
                return true;
            });

        var validatorInvoker = Substitute.For<IValidatorInvoker>();
        validatorInvoker.Invoke(default!, default!, default!, default)
            .ReturnsForAnyArgs(x =>
            {
                visits.Add($"{x[0].GetType().Name}@{x[2]}");
                return Task.FromResult<IEnumerable<Validation.ValidationResult>>([]);
            });

        var root = assembly.GetType("Walked.Samples")!.GetMethod("Create")!.Invoke(null, null)!;
        new ModelGraphValidator(discoverableValidators, validatorInvoker).Validate(new ModelGraphValidationRequest(root)).GetAwaiter().GetResult();
        return string.Join(" | ", visits);
    }

    ModelGraphMember[] GeneratedMembersOf(string type)
    {
        ModelGraphWalkers.TryGet(_generated.GetType(type)!, out var members).ShouldBeTrue();
        return members!;
    }
}
