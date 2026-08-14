### New Rules
Rule ID|Category|Severity|Notes
--------|----------|----------|--------------------
ARCCHR0001|Arc.Chronicle|Error|Incorrect AggregateRoot event handler signature
ARCCHR0002|Arc.Chronicle|Warning|Command has ambiguous event source id and should implement ICanProvideEventSourceId
ARCCHR0003|Arc.Chronicle|Warning|Reactor must not reach the default event log
ARCCHR0004|Arc.Chronicle|Warning|[EventType] should not specify an explicit id
ARCCHR0005|Arc.Chronicle|Warning|Chronicle artifacts are present but Chronicle is not wired up
ARCCHR0006|Arc.Chronicle|Warning|Reactor handler invoking ICommandPipeline.Execute must be marked with [OnceOnly]
ARCCHR0007|Arc.Chronicle|Warning|Command handler must not inject IEventLog
ARCCHR0008|Arc.Chronicle|Warning|Command key marked with the data annotations Key attribute
