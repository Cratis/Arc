// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands.for_CommandScenario;
using Cratis.Arc.Chronicle.Testing.Slices;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Slices.for_Slice.given;

public class a_slice : Specification
{
    protected EventSourceId _source;
    protected Slice<SettleLedger> _slice;
    protected SliceOutcome<SettleLedger> _outcome;

    void Establish()
    {
        _source = EventSourceId.New();
        _slice = new(new SettleLedger(_source));
    }

    async Task Destroy() => await _slice.DisposeAsync();
}
