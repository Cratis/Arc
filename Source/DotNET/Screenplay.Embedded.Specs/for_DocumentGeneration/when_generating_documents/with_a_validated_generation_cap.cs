// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Embedded.Generation;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay.Semantics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_generating_documents;

public class with_a_validated_generation_cap : Specification
{
    [Theory]
    [InlineData(false, 19, false)]
    [InlineData(true, 19, false)]
    [InlineData(false, 20, true)]
    [InlineData(true, 20, true)]
    [InlineData(false, 0, true)]
    [InlineData(true, 0, true)]
    public void should_forward_the_v20_boundary_to_every_embedded_scope(bool authoring, int cap, bool admitted)
    {
        var source = string.Join(
            '\n',
            "using Cratis.Arc.Commands.ModelBound;",
            "using Cratis.Concepts;",
            "using FluentValidation;",
            "namespace Library.Authors.Registration;",
            "public record ReceiptId(string Value) : ConceptAs<string>(Value), IGeneratable<ReceiptId>",
            "{ public static ReceiptId New() => new(GenerateValue.NanoId()); }",
            "public class ReceiptValidator : Cratis.Arc.Validation.ConceptValidator<ReceiptId>",
            "{ public ReceiptValidator() { RuleFor(value => value.Value).NotEmpty(); } }",
            "[Command] public record AllocateReceipt",
            "{ public ReceiptId Handle([GeneratedValue] ReceiptId receipt) => receipt; }");
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(
            "Library",
            [CSharpSyntaxTree.ParseText(source, path: "Library/Receipt.cs")],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var version = cap == 19 ? SemanticVersion.V19 : cap == 20 ? SemanticVersion.V20 : (SemanticVersion?)null;
        var result = new EmbeddedDocumentGenerator(new ApplicationModelAnalyzer(), new ScreenplayEmitter(), new ScreenplayVerifier())
            .Generate(compilation, new("Library") { AuthoringOnlyConstructs = authoring, MaximumExecutableModelVersion = version });
        result.Documents.ShouldNotBeEmpty();
        foreach (var document in result.Documents)
        {
            document.Source.ShouldContain("not empty");
            document.Source.Contains("generate nanoid", StringComparison.Ordinal).ShouldEqual(admitted);
            new ScreenplayVerifier().Verify(document.Source).UnexpectedBindingErrors(authoring).ShouldBeEmpty();
        }
    }
}
