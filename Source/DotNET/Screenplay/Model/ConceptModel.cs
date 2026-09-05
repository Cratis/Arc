// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a strongly typed domain value declared at the document level.
/// </summary>
/// <param name="Name">The name of the concept.</param>
/// <param name="Primitive">The primitive the concept is backed by.</param>
/// <param name="IsPii">Whether the concept carries personally identifiable information.</param>
/// <param name="EnumValues">The enumeration values, empty unless <paramref name="Primitive"/> is <see cref="ScreenplayPrimitive.Enum"/>.</param>
/// <param name="Validations">The validation rules the concept declares for itself.</param>
/// <remarks>
/// The <see cref="ValidationRuleModel.Property"/> of every rule is ignored - inside a concept declaration the
/// concept's own value is the implied subject.
/// </remarks>
public record ConceptModel(
    string Name,
    ScreenplayPrimitive Primitive,
    bool IsPii,
    IEnumerable<string> EnumValues,
    IEnumerable<ValidationRuleModel> Validations);
