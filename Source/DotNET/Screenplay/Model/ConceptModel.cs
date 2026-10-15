// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;

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
    IEnumerable<ValidationRuleModel> Validations)
{
    /// <summary>
    /// Gets the integer identities parallel to <see cref="EnumValues"/>, empty when no numbers were recovered.
    /// </summary>
    public IEnumerable<BigInteger> EnumNumbers { get; init; } = [];

    /// <summary>
    /// Gets whether the value is encrypted and withheld from command auditing.
    /// </summary>
    public bool IsSensitive { get; init; }

    /// <summary>
    /// Gets the proven generation strategy owned by this concept.
    /// </summary>
    internal GenerationModel? Generation { get; init; }

    /// <summary>
    /// Gets whether every executable default validator rule was recovered.
    /// </summary>
    internal bool ValidationRecoveryComplete { get; init; } = true;

    /// <summary>
    /// Gets source attachments preserving validators whose UUID rules have no declarative counterpart.
    /// </summary>
    internal IReadOnlyList<ValidationRuleModel> ValidatorSources { get; init; } = [];

    /// <summary>
    /// Gets the source anchor for generation withholding diagnostics.
    /// </summary>
    internal string? GenerationLocation { get; init; }

    /// <summary>
    /// Gets the non-default encryption scope, or null for Subject scope.
    /// </summary>
    internal string? EncryptionScope { get; init; }

    /// <summary>
    /// Gets the analyzed unmarked uses affected by member-level personal-data promotion.
    /// </summary>
    internal IReadOnlyList<string> UnmarkedPiiUses { get; init; } = [];
}
