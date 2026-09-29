// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Generators.Specs.Testing;

/// <summary>
/// Represents an in-memory compiled library standing in for a project reference.
/// </summary>
/// <param name="Name">The assembly name.</param>
/// <param name="Image">The compiled assembly image.</param>
public sealed record ProjectLibrary(string Name, ImmutableArray<byte> Image)
{
    /// <summary>
    /// Gets a <see cref="MetadataReference"/> to the library, with a file path named after the assembly the way the
    /// build passes a project reference to the compiler.
    /// </summary>
    public MetadataReference Reference => MetadataReference.CreateFromImage(Image, filePath: $"{Name}.dll");

    /// <summary>
    /// Compiles a library from source.
    /// </summary>
    /// <param name="name">The assembly name.</param>
    /// <param name="source">The C# source of the library.</param>
    /// <returns>The compiled <see cref="ProjectLibrary"/>.</returns>
    /// <exception cref="InvalidOperationException">The source does not compile.</exception>
    public static ProjectLibrary Compile(string name, string source)
    {
        var compilation = CSharpCompilation.Create(
            name,
            [CSharpSyntaxTree.ParseText(source)],
            ProjectReferenceModulesGeneratorRunner.PlatformReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        return result.Success
            ? new(name, [.. stream.ToArray()])
            : throw new InvalidOperationException($"Library '{name}' does not compile: {string.Join(Environment.NewLine, result.Diagnostics)}");
    }
}
