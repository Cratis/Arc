// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents everything a Screenplay document is generated from.
/// </summary>
/// <param name="Domain">The name of the domain the document belongs to.</param>
/// <param name="Module">The name of the module every feature is placed within.</param>
/// <param name="Concepts">The concepts declared at the document level.</param>
/// <param name="Policies">The policies declared at the document level.</param>
/// <param name="Slices">Every slice of the application, flat - emission builds the feature tree from the namespaces.</param>
/// <param name="Types">The shapes declared at the document level, which artifacts carry values of.</param>
public record ApplicationModel(
    string Domain,
    string Module,
    IEnumerable<ConceptModel> Concepts,
    IEnumerable<PolicyModel> Policies,
    IEnumerable<SliceModel> Slices,
    IEnumerable<TypeModel> Types)
{
    /// <summary>
    /// Represents an application that declares nothing at all.
    /// </summary>
    public static readonly ApplicationModel Empty = new(string.Empty, string.Empty, [], [], [], []);

    /// <summary>
    /// Gets the fully qualified name of every event the application refers to that something it references declares.
    /// </summary>
    /// <remarks>
    /// An event a sibling bounded context publishes is real, but nothing here declares it, so the document states the
    /// dependency outright rather than referring to a name it never introduces.
    /// </remarks>
    public IEnumerable<string> Imports { get; init; } = [];
}
