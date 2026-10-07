// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Validation;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ExecutableValidationRules.when_applying;

public class with_concepts_carried_by_commands : Specification
{
    [Theory]
    [InlineData("AuthorName", false, false)]
    [InlineData("AuthorName", true, false)]
    [InlineData("Envelope", false, false)]
    [InlineData("Envelope", true, false)]
    [InlineData("Names", false, false)]
    [InlineData("AuthorName", false, true)]
    [InlineData("AuthorName", true, true)]
    [InlineData("Envelope", false, true)]
    [InlineData("Envelope", true, true)]
    [InlineData("Names", false, true)]
    public void should_withhold_concept_rules_for_every_exercised_carrier(string type, bool collection, bool rejection)
    {
        var model = Application(type, collection, "RegisterOtherAuthor", rejection);
        var diagnostics = new ScreenplayDiagnostics();
        var result = new ExecutableValidationRules(diagnostics).Apply(model);
        result.Concepts.Single().Validations.Select(rule => rule.Kind).ShouldContainOnly(ValidationRuleKind.NotEmpty);
        var diagnostic = diagnostics.All.Single();
        diagnostic.Code.ShouldEqual(ScreenplayDiagnosticCodes.UnmappableValidationRule);
        diagnostic.Message.ShouldContain("concept 'AuthorName'");
        diagnostic.Message.ShouldContain("RegisterOtherAuthor");
        result.Slices.Single().Specifications.ShouldEqual(model.Slices.Single().Specifications);
    }

    [Theory]
    [InlineData("UnrelatedCommand", true)]
    [InlineData("UnrelatedCommand", false)]
    public void should_keep_concept_rules_without_a_scenario_for_a_carrier(string command, bool rejection)
    {
        var diagnostics = new ScreenplayDiagnostics();
        var result = new ExecutableValidationRules(diagnostics).Apply(Application("Envelope", true, command, rejection));
        result.Concepts.Single().Validations.Count().ShouldEqual(2);
        diagnostics.All.ShouldBeEmpty();
    }

    static ApplicationModel Application(string type, bool collection, string issued, bool rejection)
    {
        ValidationRuleModel[] rules =
        [
            new("Value", ValidationRuleKind.NotEmpty, null, null),
            new("Value", ValidationRuleKind.Rule, "IsKnownName", "Use a known name") { SourceFilePath = "Authors/Names.cs" }
        ];
        var first = new CommandModel("RegisterAuthor", null, [new("Name", new("AuthorName", false, false))], null, [], [], null, null);
        var second = first with { Name = "RegisterOtherAuthor", Properties = [new("Name", new(type, collection, false))] };
        var unrelated = first with { Name = "UnrelatedCommand", Properties = [new("Name", new("String", false, false))] };
        var slice = SliceModel.Empty("Library.Authors.Registration", "Registration", SliceKind.StateChange) with
        {
            Commands = [first, second, unrelated],
            Specifications = [new("ANameIsSupplied", [], new(issued, SpecificationStateKind.Command, []), [], rejection ? ["unknown-name"] : [])]
        };

        return ApplicationModel.Empty with
        {
            Concepts = [new("AuthorName", ScreenplayPrimitive.String, false, [], rules)],
            Slices = [slice],
            Types =
            [
                new("Envelope", [new("Details", new("Names", false, false))]),
                new("Names", [new("Values", new("AuthorName", true, false)), new("Parent", new("Envelope", false, true))])
            ]
        };
    }
}
