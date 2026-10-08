// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.Embedded.Generation;

/// <summary>
/// Represents what an assembly's embedded documents are generated from.
/// </summary>
/// <param name="AssemblyName">The name of the assembly the documents are embedded in.</param>
/// <param name="RootNamespace">The namespace the hierarchy is resolved relative to.</param>
/// <remarks>
/// Both values are what the build already knows about the project, which is why neither is configured separately.
/// A project that never set <c>RootNamespace</c> has the one the SDK gave it - the assembly name - so the default
/// here is the same answer rather than a second convention.
/// </remarks>
public record EmbeddedDocumentOptions(string? AssemblyName, string? RootNamespace = null)
{
    /// <summary>
    /// Gets a value indicating whether authoring-only constructs are included in embedded documents.
    /// </summary>
    public bool AuthoringOnlyConstructs { get; init; }

    /// <summary>
    /// Gets the executable model version cap, or null to emit the latest supported constructs.
    /// </summary>
    public SemanticVersion? MaximumExecutableModelVersion { get; init; }

    /// <summary>
    /// Gets the options with every value filled in.
    /// </summary>
    /// <returns>The resolved options.</returns>
    public EmbeddedDocumentOptions Resolve()
    {
        var assembly = Fallback(AssemblyName, ScreenplayOptions.DefaultName);

        return this with
        {
            AssemblyName = assembly,
            RootNamespace = Fallback(RootNamespace, assembly)
        };
    }

    /// <summary>
    /// Gets the first of two values that carries content.
    /// </summary>
    /// <param name="value">The preferred value.</param>
    /// <param name="fallback">The value to fall back to.</param>
    /// <returns>The resolved value.</returns>
    static string Fallback(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
