// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents the command response a scenario asserts with concrete values.
/// </summary>
/// <param name="ResponseType">The fully qualified response type the scenario observed.</param>
/// <param name="Value">The expected scalar response, or <see langword="null"/> for a record expectation.</param>
/// <param name="Fields">The expected record fields, in the order the scenario asserts them; empty for a scalar expectation.</param>
/// <remarks>
/// A record expectation is a subset: only the fields the scenario compares are stated.
/// </remarks>
public record SpecificationReturnModel(string ResponseType, LiteralSource? Value, IReadOnlyList<PropertyMappingModel> Fields)
{
    /// <summary>
    /// Determines whether the expectation has the shape and type of a command's recovered response.
    /// </summary>
    /// <param name="authoring">The recovered command values and response, if any.</param>
    /// <returns>True when the command declares this response and every asserted field.</returns>
    public bool Fits(CommandAuthoringModel? authoring) =>
        authoring is { ResponseType: { } type } && string.Equals(type, ResponseType, StringComparison.Ordinal) &&
        (Value is not null
            ? authoring.Response is not null && Fields.Count == 0
            : Fields.Count > 0 && authoring.ResponseFields.Count > 0 &&
              Fields.All(field => authoring.ResponseFields.Any(declared => string.Equals(declared.Property, field.Property, StringComparison.Ordinal))));
}
