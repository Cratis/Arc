### New Rules
Rule ID|Category|Severity|Notes
--------|----------|----------|--------------------
ARC0001|Arc|Error|Incorrect Query method signature on ReadModel
ARC0002|Arc|Warning|Missing [Command] attribute on command-like type
ARC0003|Arc|Error|Handle() must be on [Command] type
ARC0004|Arc|Error|[Command] type must have public Handle() method
ARC0005|Arc|Warning|Value produced by Provide is not consumed by Handle
ARC0006|Arc|Warning|Command-scoped read model can be missing
ARC0007|Arc|Warning|Command should be declared as a record
ARC0008|Arc|Warning|ReadModel should be declared as a record
ARC0009|Arc|Warning|Concept should be declared as a record
ARC0010|Arc|Warning|Command Handle() wraps a synchronous result in a Task
ARC0011|Arc|Warning|[Roles] argument should use nameof instead of a string literal
ARC0012|Arc|Warning|Arc artifact throws a built-in exception type
ARC0013|Arc|Warning|Validator rule dereferences a possibly-null concept member
ARC0014|Arc|Error|Generic query method on ReadModel
ARC0015|Arc|Warning|Query parameter converted to a concept in the method body
ARC0016|Arc|Error|Invalid command operation method
ARC0017|Arc|Error|Use CommandOperations for operation batches
ARC0018|Arc|Error|Operation cannot have a generated invoker
ARC0019|Arc|Warning|[AllowAnonymous] conflicts with [Authorize] or [Roles] on the same declaration
ARC0020|Arc|Warning|ASP.NET Core authorization attribute is not enforced without Arc's ASP.NET Core integration
ARC0021|Arc|Warning|Authorization setting is not evaluated on a model-bound Arc artifact
