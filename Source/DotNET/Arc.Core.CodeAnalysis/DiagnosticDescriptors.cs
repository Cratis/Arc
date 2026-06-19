// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.CodeAnalysis;

/// <summary>
/// Diagnostic descriptors for Arc analyzers.
/// </summary>
static class DiagnosticDescriptors
{
    /// <summary>
    /// ARC0001: Incorrect Query method signature on ReadModel.
    /// </summary>
    public static readonly DiagnosticDescriptor ARC0001_IncorrectQueryMethodSignature = new(
        id: "ARC0001",
        title: "Incorrect Query method signature on ReadModel",
        messageFormat: "Query method '{0}' on ReadModel '{1}' returns '{2}'. Change it to return '{1}', a collection of '{1}', Task<{1}>, Task<IEnumerable<{1}>>, IAsyncEnumerable<{1}>, or ISubject<{1}>.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Fix by changing the query method return type so it returns the read model itself or one of the allowed wrappers/collections of the same read model type.");

    /// <summary>
    /// ARC0002: Missing [Command] attribute on command-like type.
    /// </summary>
    public static readonly DiagnosticDescriptor ARC0002_MissingCommandAttribute = new(
        id: "ARC0002",
        title: "Missing [Command] attribute on command-like type",
        messageFormat: "Type '{0}' has a Handle method but is missing [Command]. Add [Command] to '{0}' or rename/remove Handle if it is not a command.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Fix by annotating the type with [Command] when it is intended to be a model-bound command. If not a command, avoid a public Handle pattern that looks like a command handler.");

    /// <summary>
    /// ARC0003: Handle() must be on [Command] type.
    /// </summary>
    public static readonly DiagnosticDescriptor ARC0003_HandleMustBeOnCommandType = new(
        id: "ARC0003",
        title: "Handle() must be on [Command] type",
        messageFormat: "Type '{0}' defines Handle() for command '{1}'. Move this logic to a public instance Handle() method on '{1}'.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Fix by placing command handling logic on the [Command] type itself. External handler classes should not define public Handle methods that take a [Command] parameter.");

    /// <summary>
    /// ARC0004: [Command] type must have public Handle().
    /// </summary>
    public static readonly DiagnosticDescriptor ARC0004_CommandMustHavePublicHandle = new(
        id: "ARC0004",
        title: "[Command] type must have public Handle() method",
        messageFormat: "Command type '{0}' must declare a public instance Handle() method. Add public Handle() on '{0}' or remove [Command] if it is not a command.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Fix by adding a public instance Handle() method to the [Command] type. Non-public Handle methods do not satisfy the model-bound command contract.");

    /// <summary>
    /// ARC0005: Value produced by Provide is not consumed by Handle.
    /// </summary>
    public static readonly DiagnosticDescriptor ARC0005_ProvidedValueNotConsumed = new(
        id: "ARC0005",
        title: "Value produced by Provide is not consumed by Handle",
        messageFormat: "Value of type '{0}' produced by the Provide method on command '{1}' is not consumed by any Handle parameter. Add a matching Handle parameter or remove the value from Provide.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Fix by adding a Handle parameter that consumes the value the Provide method returns, or by removing the unused value from Provide. Control values (ValidationResult, AuthorizationResult, CommandResult) are exempt because they short-circuit execution rather than feed Handle.");

    /// <summary>
    /// ARC0006: Command-scoped read model can be missing.
    /// </summary>
    public static readonly DiagnosticDescriptor ARC0006_CommandScopedReadModelCanBeMissing = new(
        id: "ARC0006",
        title: "Command-scoped read model can be missing",
        messageFormat: "Read model parameter '{0}' on '{1}' is non-nullable, but command-scoped read models can be missing",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Make the read model parameter nullable when absence is part of the command's valid behavior. Keep it non-nullable when absence should fail as a required dependency, or inject IReadModels for explicit existence checks.");

    /// <summary>
    /// ARC0007: Command should be declared as a record.
    /// </summary>
    public static readonly DiagnosticDescriptor ARC0007_CommandShouldBeRecord = new(
        id: "ARC0007",
        title: "Command should be declared as a record",
        messageFormat: "Command '{0}' is declared as a class. Declare it as a record for value equality, immutability, and concise syntax.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Commands are immutable data structures and should be declared as records. Records give value equality, immutability, and concise positional syntax that the model-bound command pipeline relies on. Change the 'class' declaration to 'record'.");

    /// <summary>
    /// ARC0008: ReadModel should be declared as a record.
    /// </summary>
    public static readonly DiagnosticDescriptor ARC0008_ReadModelShouldBeRecord = new(
        id: "ARC0008",
        title: "ReadModel should be declared as a record",
        messageFormat: "ReadModel '{0}' is declared as a class. Declare it as a record for value equality, immutability, and concise syntax.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Read models are immutable projections of events and should be declared as records. Records give value equality, immutability, and concise positional syntax. Change the 'class' declaration to 'record'.");

    /// <summary>
    /// ARC0009: Concept should be declared as a record.
    /// </summary>
    public static readonly DiagnosticDescriptor ARC0009_ConceptShouldBeRecord = new(
        id: "ARC0009",
        title: "Concept should be declared as a record",
        messageFormat: "Concept '{0}' inherits ConceptAs<T> but is declared as a class. Declare it as a record so value equality and immutability work as intended.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Concepts inherit ConceptAs<T> to wrap a primitive in a strongly-typed domain value. They must be declared as positional records so that value equality, immutability, and the implicit conversions behave correctly. Change the 'class' declaration to 'record'.");

    const string Category = "Arc";
}
