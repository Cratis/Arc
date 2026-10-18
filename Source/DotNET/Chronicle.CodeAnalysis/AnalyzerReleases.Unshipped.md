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
ARCCHR0023|Arc.Chronicle|Warning|Read-model lookup ambiguity; redirects outside On and repeated keys report Info
ARCCHR0024|Arc.Chronicle|Warning|Statically refused read-model shape in Measure or On
ARCCHR0025|Arc.Chronicle|Warning|Keyless command reads Chronicle state; IReadModels and IEventStore report Info
ARCCHR0026|Arc.Chronicle|Info|Chronicle model read through a MongoDB collection; Warning under On
ARCCHR0027|Arc.Chronicle|Warning|Refusal branch must return registered events or acknowledge
ARCCHR0028|Arc.Chronicle|Warning|Raw Chronicle reads and explicit bypasses under On
ARCCHR0029|Arc.Chronicle|Error|Conflicting ProtectedDecision, ReadProtection and Unprotected declarations
ARCCHR0031|Arc.Chronicle|Error|LookUpBy requires a read-model parameter and a public command property
ARCCHR0032|Arc.Chronicle|Info|Inert or redundant ReadProtection and Unprotected declarations
ARCCHR0033|Arc.Chronicle|Warning|Aggregate and protected plain read on the same key under On
ARCCHR0034|Arc.Chronicle|Info|Method-level advisory does not cover read-model parameters
ARCCHR0035|Arc.Chronicle|Warning|Aggregate Commit in nested command or before later reads/appends under On
ARCCHR0036|Arc.Chronicle|Warning|Exact scope coverage of same-source protected read is not proven
ARCCHR0037|Arc.Chronicle|Error|Per-stream read model does not match the command route
ARCCHR0038|Arc.Chronicle|Info|Per-stream read guard widens to the whole source
ARC0026|Arc|Error|Invalid explicit query declaration
ARC0030|Arc|Info|Composite key requires accessor registration
