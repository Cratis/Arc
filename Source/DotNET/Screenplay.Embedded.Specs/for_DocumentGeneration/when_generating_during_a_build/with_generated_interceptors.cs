// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Build;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_generating_during_a_build;

public class with_generated_interceptors : Specification, IDisposable
{
    string _directory;
    given.a_build _build;
    GenerateEmbeddedScreenplayDocuments _task;
    bool _result;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"cratis-interceptors-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        var source = Path.Combine(_directory, "Calls.cs");
        const string authored = "namespace Library { public static class CompilerFeatureCalls { public static string Run() => CompilerFeatureCalls.Current() + CompilerFeatureCalls.Legacy(); public static string Current() => \"original\"; public static string Legacy() => \"original\"; } } " +
            "namespace System.Runtime.CompilerServices { [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)] public sealed class InterceptsLocationAttribute(int version, string data) : System.Attribute { } }";
        File.WriteAllText(source, authored);
        _build = new();
        _task = new()
        {
            BuildEngine = _build,
            AssemblyName = "Library",
            LanguageVersion = "preview",
            Features = "embedded-test=forwarded",
            InterceptorsNamespaces = "EmbeddedInterceptors.Current",
            InterceptorsPreviewNamespaces = "EmbeddedInterceptors.Legacy",
            Sources = [new TaskItem(source)],
            References = [.. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Select(_ => (ITaskItem)new TaskItem(_))],
            Analyzers = [new TaskItem(typeof(EmbeddedInterceptors.Generator.Interceptors).Assembly.Location)],
            OutputPath = Path.Combine(_directory, "generated")
        };
    }

    void Because() => _result = _task.Execute();

    [Fact] void should_generate_documents() => _result.ShouldBeTrue();
    [Fact] void should_report_no_compilation_errors() => _build.Errors.ShouldBeEmpty();
    [Fact] void should_report_no_compilation_warnings_for_the_generated_interceptors() => _build.Warnings.ShouldBeEmpty();
    [Fact] void should_write_both_the_document_and_catalog() => _task.Documents.Length.ShouldEqual(2);

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Directory.Delete(_directory, true);
    }
}
