// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_pinned_validated_generation : a_pinned_validated_value
{
    const string Rule = "RuleFor(value => value.Value).Matches(\"^[ab]{64}$\");";
    const string Scenario = "WhenAllocatingAndAReceiptIsPinned";

    [Fact]
    void should_execute_a_valid_pin_without_deleting_its_rule()
    {
        GeneratePin(new string('a', 64), Rule);
        Result.Source.ShouldContain("matches \"^[ab]{64}$\"");
        Result.Source.ShouldContain($"specification {Scenario}");
        AssertDocument();
        Run(Scenario).Passed.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_withhold_a_success_scenario_with_a_pin_the_concept_cannot_generate(bool authoring)
    {
        GeneratePin(new string('a', 63) + "c", Rule, new() { AuthoringOnlyConstructs = authoring });
        Result.Source.ShouldContain("generate nanoid");
        Result.Source.ShouldContain("matches \"^[ab]{64}$\"");
        Result.Source.ShouldNotContain($"specification {Scenario}");
        var diagnostic = Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification);
        diagnostic.Message.ShouldContain("PinGeneratedValue('receipt')");
        diagnostic.Location.ShouldNotBeNull();
        AssertPartialDocument();
    }

    [Fact]
    void should_keep_opaque_rules_instead_of_weakening_them_for_a_scenario()
    {
        GeneratePin(new string('a', 64), "RuleFor(value => value.Value).Must(Approved);");
        Result.Source.ShouldContain("rule Approved");
        Result.Source.ShouldNotContain("generated");
        Result.Source.ShouldNotContain($"specification {Scenario}");
        Result.Diagnostics.Where(diagnostic => diagnostic.Message.Contains("withheld to keep scenarios", StringComparison.Ordinal)).ShouldBeEmpty();
        AssertPartialDocument();
    }

    [Fact]
    void should_classify_an_invalid_generated_value_as_a_server_contract_defect()
    {
        GeneratePin(new string('a', 64), Rule);
        var source = Result.Source.Replace(new string('a', 64), new string('a', 63) + "c", StringComparison.Ordinal);
        const string Key = "application";
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Library"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(Key), Key, "application.play", source);
        var bound = new SemanticModelCompiler().Compile("Library", SemanticDocumentSet.Create([document], catalog));
        bound.Success.ShouldBeTrue();
        var plan = SemanticExecutionPlan.Compile(bound.Value!.Model).Plan!;
        var execution = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single().Id).Execution;
        execution.ShouldBeOfExactType<SemanticRejected>();
        ((SemanticRejected)execution).Category.ShouldEqual(SemanticRejectionCategory.Contract);
    }
}
