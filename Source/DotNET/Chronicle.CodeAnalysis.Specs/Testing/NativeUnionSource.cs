// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;

public static class NativeUnionSource
{
    public const string Imports = "using System; using System.Collections.Generic; using System.Threading.Tasks; " +
        "using Cratis.Arc.Commands.ModelBound; using Cratis.Arc.Validation; using Cratis.Chronicle.Events; " +
        "using Cratis.Chronicle.EventSequences; using Cratis.Monads; using OneOf;";

    public const string Events = "[EventType] public record E; public record Failure; public union Choice<T, U>(T, U);";

    public static Task Verify<TAnalyzer>(string source, params ExpectedDiagnostic[] expected)
        where TAnalyzer : DiagnosticAnalyzer, new() =>
        AnalyzerVerifier<TAnalyzer>.VerifyAnalyzerAsync(source, expected);

    public static async Task<MetadataReference> Emit(string source)
    {
        var compilation = (await TestProject.CreateProject(source).GetCompilationAsync())!
            .WithAssemblyName("NativeUnionMetadata");
        await using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        if (!result.Success)
        {
            throw new SnippetDoesNotCompile(result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        }
        return MetadataReference.CreateFromImage(stream.ToArray());
    }
}
#endif
