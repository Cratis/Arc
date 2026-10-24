// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.for_RefusalBranchReturnAnalyzer.given;
using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.RefusalBranchReturnAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_RefusalBranchReturnAnalyzer.when_the_return_is_opaque;

public class with_a_helper_call : a_refusal_callback
{
    [Fact]
    public async Task should_leave_helper_results_to_runtime_validation() => await VerifyCS.VerifyAnalyzerAsync(Callback("_ => Produce()", "object Produce() => new DoWork();"));

    [Fact]
    public async Task should_not_inspect_unrelated_nested_returns() => await VerifyCS.VerifyAnalyzerAsync(Callback("_ => { Func<object> unrelated = () => new DoWork(); object Local() => new object(); return new Recorded(); }"));

    [Fact]
    public async Task should_leave_erased_values_to_runtime_validation() => await VerifyCS.VerifyAnalyzerAsync(Callback("_ => _value", "object _value = new Recorded();"));

    [Fact]
    public async Task should_leave_other_compilations_callbacks_opaque()
    {
        var external = (await TestProject.CreateProject("public static class External { public static object Produce(Cratis.Arc.Commands.CommandRefusal refusal) => new object(); }").GetCompilationAsync(global::Xunit.TestContext.Current.CancellationToken))!
            .WithAssemblyName("ExternalCallbacks");
        var compilation = (await TestProject.CreateProject(Callback("External.Produce")).GetCompilationAsync(global::Xunit.TestContext.Current.CancellationToken))!
            .AddReferences(external.ToMetadataReference());
        compilation.GetDiagnostics(global::Xunit.TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        var diagnostics = await compilation.WithAnalyzers([new RefusalBranchReturnAnalyzer()]).GetAnalyzerDiagnosticsAsync(global::Xunit.TestContext.Current.CancellationToken);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task should_not_infer_a_custom_collection_builders_output() => await VerifyCS.VerifyAnalyzerAsync(Callback("_ => (EventBatch)[]") + @"
        [System.Runtime.CompilerServices.CollectionBuilder(typeof(EventBatchBuilder), nameof(EventBatchBuilder.Create))]
        public sealed class EventBatch : IEnumerable<Recorded>
        {
            public IEnumerator<Recorded> GetEnumerator() { yield return new Recorded(); }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }
        public static class EventBatchBuilder
        {
            public static EventBatch Create(ReadOnlySpan<Recorded> values) => new();
        }
        ");

    [Theory]
    [InlineData("new[] { value }")]
    [InlineData("new List<T> { value }")]
    public async Task should_not_classify_unknown_generic_elements(string batch) => await VerifyCS.VerifyAnalyzerAsync(Preamble + $@"
        public class GenericReactor<T>
        {{
            public CommandInvocation Handle(T value) => Invoke.Command(new DoWork()).OnRefused(Refused.Any, _ => {batch});
        }}
        ");
}
