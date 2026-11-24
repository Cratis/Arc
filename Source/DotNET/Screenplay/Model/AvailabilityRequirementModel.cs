// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// A requirement of an <c>available</c> or <c>availability</c> line, as the code states it.
/// </summary>
public abstract record AvailabilityRequirementModel
{
    /// <summary>
    /// Gets the names of the audiences and availabilities the requirement refers to.
    /// </summary>
    public abstract IEnumerable<string> Names { get; }

    /// <summary>
    /// Gets the forms the requirement is made of: <see langword="for"/>, <see langword="with"/> and <see langword="when"/>.
    /// </summary>
    /// <returns>The distinct forms.</returns>
    public abstract IReadOnlySet<AvailabilityForm> Forms();

    /// <summary>
    /// Splits the requirement into lines that each hold one form.
    /// </summary>
    /// <returns>The lines, which all have to hold, or <see langword="null"/> when forms are mixed beneath an <c>or</c> or a <c>not</c>, which no line can state.</returns>
    public IReadOnlyList<AvailabilityRequirementModel>? Lines()
    {
        if (Forms().Count <= 1)
        {
            return [this];
        }

        if (this is not LogicalRequirementModel { IsAnd: true } logical)
        {
            return null;
        }

        var left = logical.Left.Lines();
        var right = logical.Right.Lines();

        return left is null || right is null ? null : [.. left, .. right];
    }
}
