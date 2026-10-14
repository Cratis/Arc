// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public record SelfReducedReadModel(int Total)
{
    public static SelfReducedReadModel Reduce(AmountAdded @event, SelfReducedReadModel? current) => new((current?.Total ?? 0) + @event.Amount);

    [EventType]
    public record AmountAdded(int Amount);
}
