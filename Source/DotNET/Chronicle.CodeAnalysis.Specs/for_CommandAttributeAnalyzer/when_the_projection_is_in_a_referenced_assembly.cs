// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandAttributeAnalyzer;

public class when_the_projection_is_in_a_referenced_assembly : Specification
{
    ImmutableArray<Diagnostic> _diagnostics;

    async Task Because()
    {
        var referenced = await TestProject.CreateProject(
            "using Cratis.Chronicle.Projections; public class Model; " +
            "public class Projection : IProjectionFor<Model> { public void Define(IProjectionBuilderFor<Model> builder) { } }")
            .GetCompilationAsync(TestContext.Current.CancellationToken);
        await using var image = new MemoryStream();
        referenced!.WithAssemblyName("ReferencedProjection").Emit(image, cancellationToken: TestContext.Current.CancellationToken).Success.ShouldBeTrue();
        var project = TestProject.CreateProject("using Cratis.Arc.Commands; public class Command { public void Handle([LookUpBy<CallerSubject>] Model model) { } }")
            .AddMetadataReference(MetadataReference.CreateFromImage(image.ToArray()));
        var compilation = await project.GetCompilationAsync(TestContext.Current.CancellationToken);
        compilation!.GetDiagnostics(TestContext.Current.CancellationToken).Where(_ => _.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        _diagnostics = await compilation.WithAnalyzers([new CommandAttributeAnalyzer()]).GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
    }

    [Fact] void should_not_require_source_visible_chronicle_backing() => _diagnostics.ShouldNotContain(_ => _.Id == "ARCCHR0031");
}
