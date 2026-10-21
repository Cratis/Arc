// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Analysis.Specifications;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.for_SpecificationValues.when_reading;

public class with_an_unproven_case_conversion : Specification
{
    [Theory]
    [InlineData(null, "parameter 'value' has a value the target fixture cannot admit")]
    [InlineData("urgent", "it cannot prove that a user-defined conversion for 'Act.Label' forwards its backing value unchanged")]
    public void should_keep_a_binding_failure_ahead_of_the_conversion_failure(string? constant, string expected)
    {
        var compilation = Analyzed.Compile(("Cases.cs", """
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Concepts;
            public record Label(string Value) : ConceptAs<string>(Value)
            {
                public static implicit operator Label(string value) => new(value.ToUpperInvariant());
            }
            [Command] public record Act(Label Label) { public void Handle() { } }
            public abstract class Cases(string value) { public Act Make() => new Act(value); }
            """));
        Analyzed.ErrorsIn(compilation).ShouldBeEmpty();
        var tree = compilation.SyntaxTrees.Single(tree => tree.FilePath == "Cases.cs");
        var model = compilation.GetSemanticModel(tree);
        var declaration = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
        var parameter = (IParameterSymbol)model.GetDeclaredSymbol(declaration.ParameterList!.Parameters.Single())!;
        var creation = declaration.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single();
        var models = new SemanticModels([compilation]);
        var draft = new SpecificationDraft { CaseValues = new([parameter], new Dictionary<string, object?> { ["value"] = constant }) };
        var values = new SpecificationValues(new(), new GeneratedIdentities(models), models);
        values.Read(creation, model, model.GetTypeInfo(creation).Type!, "Case", "Cases", draft).ShouldBeEmpty();
        draft.Unreadable.ShouldEqual(expected);
    }
}
