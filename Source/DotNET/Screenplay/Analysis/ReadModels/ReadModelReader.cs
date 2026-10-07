// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.ReadModels;

/// <summary>
/// Reads what is known about a read model into the catalog of the whole application.
/// </summary>
/// <param name="paths">The <see cref="SourcePaths"/> the file declaring a read model is written relative to.</param>
/// <param name="catalog">The <see cref="ReadModelCatalog"/> of the whole application.</param>
public class ReadModelReader(SourcePaths paths, ReadModelCatalog catalog)
{
    /// <summary>
    /// Declares a read model, once per type however often it is reached.
    /// </summary>
    /// <param name="type">The read model type.</param>
    /// <remarks>
    /// Only a type the document can name is declared - a constructed generic or a type parameter is written as a
    /// single identifier that says less than the type does, so declaring a shape under it would describe nothing the
    /// application has.
    /// </remarks>
    public void Declare(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol { TypeArguments.Length: 0, TypeKind: TypeKind.Class or TypeKind.Struct } named)
        {
            return;
        }

        var fullName = named.ToDisplayString();
        if (catalog.Contains(fullName))
        {
            return;
        }

        catalog.Declare(fullName, new(named, new(named.Name, [])
        {
            Namespace = named.Namespace(),
            Description = Documentation.SummaryOf(named),
            File = PortablePathOf(named)
        }));
    }

    /// <summary>
    /// Gets the path of the file declaring a type, when it can be written into a document that is committed.
    /// </summary>
    /// <param name="type">The type to locate.</param>
    /// <returns>The relative path, or <see langword="null"/> when there is none that stays true on another machine.</returns>
    /// <remarks>
    /// A type that arrives as metadata has no file, and one emitted by a source generator lives in a build folder
    /// nobody commits. Either would name a file that is not in the repository, so neither is named at all.
    /// </remarks>
    string? PortablePathOf(INamedTypeSymbol type)
    {
        var source = type.SourceFilePath();
        if (source is null || GeneratedSource.Is(source))
        {
            return null;
        }

        var path = paths.Relative(source);

        return path is not null &&
            !Path.IsPathRooted(path) &&
            !path.Contains(':', StringComparison.Ordinal) &&
            !path.Split('/').Contains("..", StringComparer.Ordinal)
            ? path
            : null;
    }
}
