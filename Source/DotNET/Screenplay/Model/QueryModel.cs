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
    bool IsObservable = false);
