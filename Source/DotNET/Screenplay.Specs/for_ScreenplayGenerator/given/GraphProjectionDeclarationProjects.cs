// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>Builds real project references with graph records exposed as emitted metadata.</summary>
public static class GraphProjectionDeclarationProjects
{
    public static Compilation Graphs(params string[] markers) =>
        Analyzed.Project("Library.Graphs", [], GraphProjectionDeclarationSources.Files(markers));

    public static Compilation Referencing(Compilation records, params string[] markers)
    {
        using var stream = new MemoryStream();
        var emitted = records.Emit(stream);
        if (!emitted.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, emitted.Diagnostics));
        }

        return Analyzed.Project(
            "Library.Declarations",
            [MetadataReference.CreateFromImage(stream.ToArray())],
            markers.Select((marker, index) => ($"Library/Declarations/Marker{index}.cs", marker)).ToArray());
    }
}
