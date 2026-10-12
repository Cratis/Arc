// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.generated_values.given;

[Command]
public record ObserveGeneration
{
    public GenerationObservation Handle([GeneratedValue] ContextId id, CommandContext context, TimeProvider clock, ITenantIdAccessor tenant) =>
        new(id, context.ReceivedAt, clock.GetUtcNow(), clock.GetLocalNow(), tenant.Current);
}
