// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Renders alternatives as short text for specifications: audiences then conditions joined with '+', alternatives joined with '|'.
/// </summary>
public static class AlternativeText
{
    public static string Of(IEnumerable<AvailabilityAlternative> alternatives) =>
        string.Join('|', alternatives.Select(_ => string.Join('+', _.Audiences.Select(a => a.Value).Concat(_.Conditions.Select(c => $"[{c}]")))));
}
