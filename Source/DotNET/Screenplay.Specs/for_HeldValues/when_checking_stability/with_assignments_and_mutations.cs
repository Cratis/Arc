// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Analysis.Specifications;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_HeldValues.when_checking_stability;

public class with_assignments_and_mutations : Specification
{
    const string Source = """
        public class Values
        {
            public int Initialized = 1;
            public int Assigned;
            public int Reassigned = 1;
            public int Incremented = 1;
            public int Decremented = 1;
            public int PassedByReference = 1;
            public int PassedAsOutput = 1;
            public int AutoProperty { get; set; } = 1;
            public int ComputedProperty => 1;
            public int CustomGetter { get { return 1; } }
            public int CustomSetter { get => 1; set { } }
            public void Change()
            {
                Assigned = 1;
                Reassigned = 2;
                Incremented++;
                --Decremented;
                Touch(ref PassedByReference);
                Fill(out PassedAsOutput);
            }
            static void Touch(ref int value) { }
            static void Fill(out int value) => value = 2;
        }
        public record Positional(int Value)
        {
            public Positional() : this(1) { Value++; }
        }
        """;

    Dictionary<string, bool> _stable = null!;

    void Because()
    {
        var compilation = Analyzed.Compile(("Values.cs", Source));
        var held = new HeldValues(new SemanticModels([compilation]));
        var type = compilation.GetTypeByMetadataName("Values")!;
        _stable = type.GetMembers().Where(member => member is IFieldSymbol or IPropertySymbol)
            .ToDictionary(member => member.Name, member => held.IsStable(member, compilation), StringComparer.Ordinal);
        var positional = compilation.GetTypeByMetadataName("Positional")!.GetMembers("Value").Single();
        _stable["Positional"] = held.IsStable(positional, compilation);
    }

    [Fact] void should_accept_one_initializer() => _stable["Initialized"].ShouldBeTrue();
    [Fact] void should_accept_one_unconditional_assignment() => _stable["Assigned"].ShouldBeTrue();
    [Fact] void should_reject_a_second_assignment() => _stable["Reassigned"].ShouldBeFalse();
    [Fact] void should_reject_postfix_increment() => _stable["Incremented"].ShouldBeFalse();
    [Fact] void should_reject_prefix_decrement() => _stable["Decremented"].ShouldBeFalse();
    [Fact] void should_reject_reference_arguments() => _stable["PassedByReference"].ShouldBeFalse();
    [Fact] void should_reject_output_arguments() => _stable["PassedAsOutput"].ShouldBeFalse();
    [Fact] void should_accept_an_initialized_auto_property() => _stable["AutoProperty"].ShouldBeTrue();
    [Fact] void should_reject_an_expression_property() => _stable["ComputedProperty"].ShouldBeFalse();
    [Fact] void should_reject_a_custom_getter() => _stable["CustomGetter"].ShouldBeFalse();
    [Fact] void should_reject_a_custom_setter() => _stable["CustomSetter"].ShouldBeFalse();
    [Fact] void should_reject_a_mutated_positional_record_property() => _stable["Positional"].ShouldBeFalse();
}
