// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Queries;

/// <summary>
/// Catalogues every query the application itself declares, for matching a generated specification's call against.
/// </summary>
/// <remarks>
/// Only queries the application declares - never a specification project - are catalogued. A specification calling
/// a lookalike declared only where specifications live is calling something the application never serves, and the
/// call is left unmatched rather than admitted on the strength of a name alone.
/// </remarks>
public static class SpecificationQueryCatalog
{
    /// <summary>
    /// Builds the catalog of every query an application declares.
    /// </summary>
    /// <param name="context">The analyzed source context.</param>
    /// <returns>The catalog, one entry per declared query.</returns>
    public static IReadOnlyList<Entry> From(DotNetAnalysisContext context) =>
        [.. context.Projects
            .Where(project => project.Role == DotNetProjectRole.Application)
            .SelectMany(project => ArtifactCatalog.From(project.Compilation).Types)
            .Where(QueryReader.IsReadModel)
            .SelectMany(readModel => QueryReader.MethodsOf(readModel)
                .Select(method => new Entry(readModel, method, DotNetMethodSignatures.From(method))))];

    /// <summary>
    /// Represents one exact query the application declares.
    /// </summary>
    /// <param name="ReadModel">The read model exposing it.</param>
    /// <param name="Method">The exact application method.</param>
    /// <param name="Signature">The exact cross-compilation signature.</param>
    public sealed record Entry(INamedTypeSymbol ReadModel, IMethodSymbol Method, DotNetMethodSignature Signature);
}
