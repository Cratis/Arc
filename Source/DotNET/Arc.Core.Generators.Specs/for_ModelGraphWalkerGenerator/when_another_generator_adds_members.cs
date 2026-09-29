// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Validation;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_ModelGraphWalkerGenerator;

/// <summary>
/// Source generators do not see each other's output, so a property another generator adds to a partial model is
/// invisible to the walker generator. The model must keep the reflection walk, which reaches the added property.
/// </summary>
public class when_another_generator_adds_members : Specification
{
    const string Models = """
        using Cratis.Arc.Commands.ModelBound;
        namespace Walked;
        public record Item(string Name);
        public partial record Extended(Item Declared);
        public record Labelled(Item Label);
        public record Tagged(Item Label, Item Tag) : Labelled(Label);
        [Command] public record Validate(Extended Extended, Tagged Tagged) { public void Handle() { } }
        public static class Samples
        {
            public static Validate Create() => new(new Extended(new Item("declared")), new Tagged(new Item("label"), new Item("tag")));
        }
        """;

    Assembly _generated;
    Assembly _reflected;
    string _generatedTraversal;
    string _reflectedTraversal;

    void Establish()
    {
        var (source, diagnostics, output) = ModelGraphWalkerCompilation.Compile("GeneratedWithAddedMembers", Models, true, [new AddingGenerator()]);
        diagnostics.ShouldBeEmpty();
        source.ShouldNotBeEmpty();
        _generated = ModelGraphWalkerCompilation.Load(output);
        _reflected = ModelGraphWalkerCompilation.Load(ModelGraphWalkerCompilation.Compile("ReflectedWithAddedMembers", Models, false, [new AddingGenerator()]).Output);
    }

    void Because()
    {
        _generatedTraversal = ModelGraphWalkerCompilation.Walk(_generated);
        _reflectedTraversal = ModelGraphWalkerCompilation.Walk(_reflected);
    }

    [Fact] void should_walk_the_same_graph_as_reflection() => _generatedTraversal.ShouldEqual(_reflectedTraversal);
    [Fact] void should_reach_the_added_member() => _generatedTraversal.ShouldContain("String@extended.added.name");
    [Fact] void should_not_register_the_partial_model() => ModelGraphWalkers.TryGet(_generated.GetType("Walked.Extended")!, out _).ShouldBeFalse();
    [Fact] void should_register_the_command() => ModelGraphWalkers.TryGet(_generated.GetType("Walked.Validate")!, out _).ShouldBeTrue();
    [Fact] void should_register_derived_records() => ModelGraphWalkers.TryGet(_generated.GetType("Walked.Tagged")!, out _).ShouldBeTrue();
    [Fact] void should_generate_the_members_reflection_finds_for_every_registered_type() =>
        ModelGraphWalkerCompilation.Describe(_generated, ModelGraphValidator.GetWalkableProperties)
            .ShouldEqual(ModelGraphWalkerCompilation.Describe(_generated, ModelGraphValidator.GetWalkablePropertiesThroughReflection));

    /// <summary>
    /// Adds a property to the partial model the way generators such as property-changed or strongly-typed identifier
    /// generators do, in output other generators do not see.
    /// </summary>
    sealed class AddingGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context) =>
            context.RegisterSourceOutput(context.CompilationProvider, static (output, _) => output.AddSource(
                "Extended.g.cs",
                "namespace Walked;\npublic partial record Extended { public Item Added { get; init; } = new(\"added\"); }\n"));
    }
}
