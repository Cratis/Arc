// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadProtectionMeasurement.given;

public abstract class MeasuredEventLog : EventLogWithNamedTags
{
    public abstract Task<IImmutableDictionary<EventSequenceNumber, EventMetadata>> GetMetadataAt(IEnumerable<EventSequenceNumber> sequenceNumbers, CancellationToken cancellationToken = default);
}
