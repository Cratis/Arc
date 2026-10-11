// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.generated_values.given;

public record CartId(Guid Value) : ConceptAs<Guid>(Value), IGeneratable<CartId>
{
    public static CartId New() => new(GenerateValue.Uuid());
}
