// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a read side entry point onto a read model.
/// </summary>
/// <param name="Name">The name of the query.</param>
/// <param name="ReturnType">The type the query returns.</param>
/// <param name="By">The parameter identifying a single instance, if the query takes one.</param>
/// <param name="Filters">The parameters narrowing the result.</param>
/// <param name="Authorization">What the query requires of the caller, if anything.</param>
/// <param name="IsObservable">Whether the query keeps answering as the read model changes rather than answering once.</param>
public record QueryModel(
    string Name,
    TypeReferenceModel ReturnType,
    PropertyModel? By,
    IEnumerable<PropertyModel> Filters,
    AuthorizationModel? Authorization,
    bool IsObservable = false)
{
    /// <summary>
    /// Gets the full name of the type the query returns, when it was read from code.
    /// </summary>
    /// <remarks>
    /// The document names what a query returns by its simple name only, so two types sharing one are told apart by
    /// this - a query returning one of them must not be taken as reading a read model that is the other.
    /// </remarks>
    public string? ReturnTypeFullName { get; init; }

    /// <summary>
    /// Gets the query's XML summary.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the repository-relative file implementing the query; emission includes it only in authoring mode.
    /// </summary>
    public string? PerformerFile { get; init; }
}
