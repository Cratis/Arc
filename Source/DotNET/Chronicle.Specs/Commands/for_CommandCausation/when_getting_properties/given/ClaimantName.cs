// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Concepts;

namespace Cratis.Arc.Chronicle.Commands.for_CommandCausation.when_getting_properties.given;

/// <summary>
/// A concept marked as personal data, so the marking travels with the value wherever it is used rather than having
/// to be repeated on every command that carries one.
/// </summary>
/// <param name="Value">The name.</param>
[PII("The name of a person")]
public record ClaimantName(string Value) : ConceptAs<string>(Value);
