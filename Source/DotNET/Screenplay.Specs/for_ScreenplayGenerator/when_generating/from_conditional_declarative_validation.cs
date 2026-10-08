// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_conditional_declarative_validation : a_generated_document
{
    const string Rules = "RuleFor(c => c.Value).NotEmpty().Length(2, 50).WithMessage(\"Scoped\");";

    static readonly (string Declaration, string Reason, bool Retained)[] _scopes =
    [
        ("RuleSet(\"draft\", () => { RULES });", "RuleSet(\"draft\")", false),
        ("RuleSet(\"default,draft\", () => { RULES });", "RuleSet(\"default,draft\")", false),
        ("RuleSet(GetRuleSetName(), () => { RULES });", "RuleSet(GetRuleSetName())", false),
        ("RuleSet(action: () => { RULES }, ruleSetName: \"draft\");", "RuleSet(\"draft\")", false),
        ("if (Disabled()) return; RULES", "early exit 'return;'", false),
        ("if (Disabled()) return; { RULES }", "early exit 'return;'", false),
        ("if (Disabled()) throw new ValidationDisabled(); RULES", "early exit 'throw", false),
        ("if (Disabled()) return; RuleSet(\"default\", () => { RULES });", "early exit 'return;'", false),
        ("When(c => c.Value.Length > 0, () => { RULES });", "enclosing 'When' block", false),
        ("Unless(c => c.Value.Length > 0, () => { RULES });", "enclosing 'Unless' block", false),
        ("RuleSet(\"default\", () => { RULES });", "", true),
        ("RuleSet(\"DEFAULT\", () => { RULES });", "", true),
        ("RuleSet(DefaultRuleSet, () => { RULES });", "", true),
        ("RULES", "", true),
        ("System.Action unused = () => { return; }; RULES", "", true)
    ];

    public static TheoryData<string, string, bool, bool> Declarations()
    {
        var declarations = new TheoryData<string, string, bool, bool>();
        foreach (var scope in _scopes)
        {
            declarations.Add(scope.Declaration.Replace("RULES", Rules, StringComparison.Ordinal), scope.Reason, scope.Retained, false);
            declarations.Add(scope.Declaration.Replace("RULES", Rules, StringComparison.Ordinal), scope.Reason, scope.Retained, true);
        }

        return declarations;
    }

    [Theory]
    [MemberData(nameof(Declarations))]
    public void should_emit_only_unconditional_command_and_concept_rules(string declaration, string reason, bool retained, bool concept)
    {
        var validatedType = concept ? "AuthorName" : "RegisterAuthor";
        var baseType = concept ? "ConceptValidator" : "CommandValidator";
        var source = $$"""
            using Cratis.Arc.Commands;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Arc.Validation;
            using Cratis.Chronicle.Events;
            using Cratis.Concepts;
            using FluentValidation;
            namespace Library.Authors.Registration;
            public class ValidationDisabled : System.Exception { }
            public record AuthorName(string Value) : ConceptAs<string>(Value);
            [EventType] public record AuthorRegistered(AuthorName Name);
            [Command] public record RegisterAuthor(string Value, AuthorName Name)
            {
                public AuthorRegistered Handle() => new(Name);
            }
            public class {{validatedType}}Validator : {{baseType}}<{{validatedType}}>
            {
                public {{validatedType}}Validator()
                {
                    const string DefaultRuleSet = "default";
                    RuleFor(c => c.Value).MaximumLength(200).WithMessage("Always");
                    {{declaration}}
                }
                static bool Disabled() => System.Environment.GetEnvironmentVariable("X") == "1";
                static string GetRuleSetName() => "default";
            }
            """;
        Analyzed.ErrorsIn((Analyzed.SlicePath, source)).ShouldBeEmpty();
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldContain("max 200 message \"Always\"");
        Result.Source.Contains("not empty", StringComparison.Ordinal).ShouldEqual(retained);
        Result.Source.Contains("min 2 message \"Scoped\"", StringComparison.Ordinal).ShouldEqual(retained);
        Result.Source.Contains("max 50 message \"Scoped\"", StringComparison.Ordinal).ShouldEqual(retained);
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableValidationRule &&
            diagnostic.Message.Contains("NotEmpty", StringComparison.Ordinal) && diagnostic.Message.Contains(reason, StringComparison.Ordinal)).ShouldEqual(!retained);
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableValidationRule &&
            diagnostic.Message.Contains("Length", StringComparison.Ordinal) && diagnostic.Message.Contains(reason, StringComparison.Ordinal)).ShouldEqual(!retained);
        AssertDocument();
    }
}
