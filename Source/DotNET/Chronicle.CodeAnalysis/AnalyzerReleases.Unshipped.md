### New Rules
Rule ID|Category|Severity|Notes
--------|----------|----------|--------------------
ARCCHR0001|Arc.Chronicle|Error|Incorrect AggregateRoot event handler signature
ARCCHR0002|Arc.Chronicle|Warning|Command has ambiguous event source id and should implement ICanProvideEventSourceId
ARCCHR0003|Arc.Chronicle|Warning|Reactor must not reach the default event log
ARCCHR0004|Arc.Chronicle|Warning|[EventType] repeats the type name as its id
ARCCHR0005|Arc.Chronicle|Warning|Chronicle artifacts are present but Chronicle is not wired up
ARCCHR0006|Arc.Chronicle|Warning|Reactor handler invoking ICommandPipeline.Execute does not say what replay should do
ARCCHR0007|Arc.Chronicle|Warning|Command handler must not inject IEventLog
ARCCHR0008|Arc.Chronicle|Warning|Command key marked with the data annotations Key attribute
ARCCHR0009|Arc.Chronicle|Warning|Command property reads as a secret and should be marked [NotAudited]
ARCCHR0010|Arc.Chronicle|Warning|Raw Guid response does not set the event source id
ARCCHR0011|Arc.Chronicle|Info|Plain read model in event-producing command is unprotected
ARCCHR0012|Arc.Chronicle|Info|Immediate append bypasses protected decision
ARCCHR0013|Arc.Chronicle|Info|Legacy event source type attribute has a matching event source definition
ARCCHR0014|Arc.Chronicle|Warning|Reactor or reducer observes a stream its event source definition does not declare
ARCCHR0015|Arc.Chronicle|Warning|Command handler must not return a nullable event
ARCCHR0016|Arc.Chronicle|Warning|Concurrency flag is ignored when the handler returns exact concurrency scopes
ARCCHR0017|Arc.Chronicle|Error|[EventStreamId] template is invalid
ARCCHR0018|Arc.Chronicle|Warning|Event stream id template is only resolved for commands
ARCCHR0019|Arc.Chronicle|Warning|Read-model fold may be exposed as a query
ARCCHR0020|Arc.Chronicle|Warning|Wrapper names a stream its event source definition does not declare
ARCCHR0021|Arc.Chronicle|Info|Legacy wrapper routing drops a matching event source definition
ARCCHR0022|Arc.Chronicle|Warning|Command-key aggregate on a generated append target
ARCCHR0023|Arc.Chronicle|Warning|Read-model lookup key is not proven; missing matches and repeated keys report Info
ARCCHR0025|Arc.Chronicle|Warning|Keyless command reads Chronicle state; IReadModels and IEventStore report Info
ARCCHR0026|Arc.Chronicle|Info|Chronicle model read through a MongoDB collection
ARC0026|Arc|Error|Invalid explicit query declaration
