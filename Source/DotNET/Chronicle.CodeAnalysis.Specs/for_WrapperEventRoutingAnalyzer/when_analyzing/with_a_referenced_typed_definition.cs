// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Cratis.Chronicle.EventSources;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_WrapperEventRoutingAnalyzer.when_analyzing;

public class with_a_referenced_typed_definition : Specification
{
    [Fact]
    async Task should_read_the_referenced_definitions_attributes()
    {
        var project = TestProject.CreateProject(@"
using Cratis.Chronicle.EventSequences;
using Cratis.Arc.Chronicle.CodeAnalysis.for_WrapperEventRoutingAnalyzer.when_analyzing;
public class Reactor
{
    public EventForEventSourceId Handle() => new(""id"", new object())
    {
        EventSource = typeof(with_a_referenced_typed_definition.ReferencedSource),
        EventStream = ""Typo""
    };
}
").AddMetadataReference(MetadataReference.CreateFromFile(typeof(ReferencedSource).Assembly.Location));
        var compilation = await project.GetCompilationAsync(global::Xunit.TestContext.Current.CancellationToken);
        compilation!.GetDiagnostics(global::Xunit.TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var diagnostics = await compilation.WithAnalyzers([new WrapperEventRoutingAnalyzer()]).GetAnalyzerDiagnosticsAsync(global::Xunit.TestContext.Current.CancellationToken);
        diagnostics.Select(diagnostic => diagnostic.Id).ShouldContainOnly("ARCCHR0020");
        diagnostics[0].GetMessage().ShouldContain("ReferencedSource");
    }

    [EventSource]
    [EventStream("Transactions")]
    public class ReferencedSource : IEventSource;
}
