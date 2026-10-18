// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Cratis.Arc.Validation;

/// <summary>
/// Describes an annotated member and its statically typed value reader, emitted by the Arc source generator.
/// </summary>
/// <param name="Name">The declared member name.</param>
/// <param name="DisplayName">The member name used in validation messages.</param>
/// <param name="ReadValue">Reads the member, unwrapping a concept to its value.</param>
/// <param name="Attributes">Lazily reads the member's validation attributes.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record DataAnnotationMember(string Name, string DisplayName, Func<object, object?> ReadValue, Func<ValidationAttribute[]> Attributes);
