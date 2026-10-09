// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Build.Generators;
using Cratis.Arc.Screenplay.Embedded.Generation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Screenplay.Embedded.for_CompilationGenerators.when_running_generators;

public class with_effective_compiler_features : Specification, IDisposable
{
    string _directory;
    Compilation _compilation;
    CompilationGeneratorResult _generated;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"cratis-features-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        var source = Path.Combine(_directory, "Calls.cs");
        const string authored = "public static class CompilerFeatureCalls { public static string Run() => CompilerFeatureCalls.Current(); public static string Current() => \"original\"; } " +
            "namespace System.Runtime.CompilerServices { [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)] public sealed class InterceptsLocationAttribute(int version, string data) : System.Attribute { } }";
        File.WriteAllText(source, authored);
        _compilation = SourceCompilation.Create(
            "InterceptedApplication",
            [source],
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator),
            "DEBUG;TRACE",
            "preview",
            "Library",
            "first;second=value, embedded-test=forwarded InterceptorsNamespaces=EmbeddedInterceptors.Current;second=last",
            "Overridden.Current",
            "Overridden.Legacy");
    }

    void Because() => _generated = CompilationGenerators.Run(
        _compilation,
        [typeof(EmbeddedInterceptors.Generator.Interceptors).Assembly.Location],
        [],
        []);

    [Fact] void should_run_the_generator() => _generated.Compilation.SyntaxTrees.Count().ShouldEqual(2);
    [Fact] void should_emit_intercepted_generated_source_without_errors() => _generated.Compilation.Emit(new MemoryStream()).Diagnostics.Where(_ => _.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_pass_the_feature_value_to_the_generator() => _generated.Compilation.SyntaxTrees.Last().ToString().Contains("Current:forwarded", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_parse_bare_features_as_true() => Options.Features["first"].ShouldEqual("true");
    [Fact] void should_use_the_last_value_for_duplicate_features() => Options.Features["second"].ShouldEqual("last");
    [Fact] void should_let_explicit_features_override_the_dedicated_namespace_properties_like_csc() => Options.Features["InterceptorsNamespaces"].ShouldEqual("EmbeddedInterceptors.Current");
    [Fact] void should_preserve_features_on_generated_syntax_trees() => _generated.Compilation.SyntaxTrees.Last().Options.Features.ShouldEqual(Options.Features);
    [Fact] void should_preserve_the_language_version() => GeneratedOptions.LanguageVersion.ShouldEqual(LanguageVersion.Preview);
    [Fact] void should_preserve_preprocessor_symbols() => GeneratedOptions.PreprocessorSymbolNames.ShouldContainOnly("DEBUG", "TRACE");

    CSharpParseOptions GeneratedOptions => (CSharpParseOptions)_generated.Compilation.SyntaxTrees.Last().Options;
    CSharpParseOptions Options => (CSharpParseOptions)_compilation.SyntaxTrees.Single().Options;

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Directory.Delete(_directory, true);
    }
}
