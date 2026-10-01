// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelViewerHosting.given;

/// <summary>
/// Builds assemblies that declare how they were built.
/// </summary>
/// <remarks>
/// Whether the explorer is exposed is read off the entry assembly's <see cref="System.Diagnostics.DebuggableAttribute"/>,
/// so the specifications need assemblies that carry the declaration a Debug build carries, the one a Release build
/// carries, and none at all.
/// </remarks>
public static class an_application_assembly
{
    /// <summary>
    /// Gets an assembly declaring what a Debug build declares - that its optimizations are disabled.
    /// </summary>
    /// <returns>The resulting assembly.</returns>
    public static Assembly BuiltForDebugging() => Emit(
        "Fixture.DebugBuild",
        "[assembly: System.Diagnostics.Debuggable(System.Diagnostics.DebuggableAttribute.DebuggingModes.Default | System.Diagnostics.DebuggableAttribute.DebuggingModes.DisableOptimizations)]");

    /// <summary>
    /// Gets an assembly declaring what a Release build declares - that it is optimized.
    /// </summary>
    /// <returns>The resulting assembly.</returns>
    public static Assembly BuiltForRelease() => Emit(
        "Fixture.ReleaseBuild",
        "[assembly: System.Diagnostics.Debuggable(System.Diagnostics.DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]");

    /// <summary>
    /// Gets an assembly that declares nothing about how it was built.
    /// </summary>
    /// <returns>The resulting assembly.</returns>
    public static Assembly WithoutADebuggableDeclaration() => Emit("Fixture.UndeclaredBuild", string.Empty);

    static Assembly Emit(string name, string source)
    {
        var compilation = CSharpCompilation.Create(
            name,
            [CSharpSyntaxTree.ParseText(source)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release));

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"The fixture assembly '{name}' could not be emitted - {string.Join(", ", result.Diagnostics.Select(diagnostic => diagnostic.ToString()))}.");
        }

        return Assembly.Load(stream.ToArray());
    }
}
