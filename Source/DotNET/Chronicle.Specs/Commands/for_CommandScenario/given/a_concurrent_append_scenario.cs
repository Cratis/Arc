// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.given;

public class a_concurrent_append_scenario : Specification
{
    protected CommandScenario<AppendWithExplicitScope> _scenario;
    protected EventSourceId _source;
    protected CommandResult _result;

    void Establish()
    {
        _scenario = new();
        _source = EventSourceId.New();
    }

    async Task Destroy() => await _scenario.DisposeAsync();

    [Command]
    public record AppendWithExplicitScope(EventSourceId EventSourceId, EventSourceId? NestedSource = default, bool Reject = false)
    {
        public async Task<object> Handle(IEventLog log, ICommandPipeline pipeline)
        {
            if (NestedSource is not null)
            {
                var nested = await pipeline.Execute(new AppendWithExplicitScope(NestedSource));
                if (!nested.IsSuccess)
                {
                    return nested;
                }
            }

            if (Reject)
            {
                return ValidationResult.Error("rejected");
            }

            var tail = await log.GetTailSequenceNumber();
            var scope = new ConcurrencyScope(tail.IsUnavailable ? EventSequenceNumber.BeforeFirst : tail, EventSourceId);
            return new EventsWithConcurrencyScopes([new(EventSourceId, new FactRecorded("owner"))], [new(EventSourceId, scope)]);
        }
    }

    [EventType]
    public record FactRecorded(string Writer);
}
