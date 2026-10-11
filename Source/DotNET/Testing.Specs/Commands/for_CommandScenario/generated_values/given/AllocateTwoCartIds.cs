// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.generated_values.given;

[Command]
public record AllocateTwoCartIds
{
    public CartId[] Handle([GeneratedValue] CartId first, [GeneratedValue] CartId second) => [first, second];
}
