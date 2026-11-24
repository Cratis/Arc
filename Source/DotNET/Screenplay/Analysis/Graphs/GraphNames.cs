// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Graphs;

/// <summary>The fully qualified Chronicle graph annotation names.</summary>
public static class GraphNames
{
    /// <summary>The Arc projection declaration marker.</summary>
    public const string ProjectionDeclaration = "Cratis.Arc.Slices.GraphProjectionDeclarationAttribute";

    /// <summary>The graph annotation namespace.</summary>
    public const string Namespace = "Cratis.Chronicle.Projections.Graphs";

    /// <summary>The named graph annotation.</summary>
    public const string Graph = $"{Namespace}.GraphAttribute";

    /// <summary>The node annotation.</summary>
    public const string Node = $"{Namespace}.NodeAttribute";

    /// <summary>The edge annotation.</summary>
    public const string Edge = $"{Namespace}.EdgeAttribute";

    /// <summary>The source endpoint annotation.</summary>
    public const string Source = $"{Namespace}.SourceAttribute`1";

    /// <summary>The target endpoint annotation.</summary>
    public const string Target = $"{Namespace}.TargetAttribute`1";

    /// <summary>The edge creation annotation.</summary>
    public const string EdgeFrom = $"{Namespace}.EdgeFromAttribute`1";

    /// <summary>The edge removal annotation.</summary>
    public const string EdgeRemoved = $"{Namespace}.EdgeRemovedWithAttribute`1";
}
