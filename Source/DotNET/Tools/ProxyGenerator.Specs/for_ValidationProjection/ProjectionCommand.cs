// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.for_ValidationProjection;

/// <summary>PRIVATE_DOCUMENTATION_8624 must not enter a projection report.</summary>
[Command]
public record ProjectionCommand(
    [property: Required(ErrorMessage = "PRIVATE_ANNOTATION_8425")] string Text,
    [property: RegularExpression("^PRIVATE_PATTERN_7426$")] string Pattern,
    int Number,
    string Conditional,
    string Async,
    string Custom,
    string Dynamic)
{
    public void Handle() { }
}
