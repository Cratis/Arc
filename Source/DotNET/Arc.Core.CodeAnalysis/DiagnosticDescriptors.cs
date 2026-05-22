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
        messageFormat: "Query method '{0}' on ReadModel '{1}' must return the ReadModel type, a collection of it, Task<ReadModel>, Task<IEnumerable<ReadModel>>, IAsyncEnumerable<ReadModel>, or ISubject<ReadModel>. Found: {2}.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Query methods on types with [ReadModel] attribute must return the ReadModel type, a collection, Task, IAsyncEnumerable, or ISubject of the ReadModel type.");

    /// <summary>
    /// ARC0002: Missing [Command] attribute on command-like type.
    /// </summary>
    public static readonly DiagnosticDescriptor ARC0002_MissingCommandAttribute = new(
        id: "ARC0002",
        title: "Missing [Command] attribute on command-like type",
        messageFormat: "Type '{0}' has a Handle method but is missing the [Command] attribute",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Types with Handle methods should have the [Command] attribute to be recognized as commands.");

    /// <summary>
    /// ARC0003: Handle() must be on [Command] type.
    /// </summary>
    public static readonly DiagnosticDescriptor ARC0003_HandleMustBeOnCommandType = new(
        id: "ARC0003",
        title: "Handle() must be on [Command] type",
        messageFormat: "Type '{0}' defines Handle() for command '{1}', but handlers must be defined on the [Command] type itself",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Public Handle methods for commands must be declared on the [Command] type itself.");

    /// <summary>
    /// ARC0004: [Command] type must have public Handle().
    /// </summary>
    public static readonly DiagnosticDescriptor ARC0004_CommandMustHavePublicHandle = new(
        id: "ARC0004",
        title: "[Command] type must have public Handle() method",
        messageFormat: "Command type '{0}' must declare a public Handle() method",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Types marked with [Command] must declare a public instance Handle() method.");

    const string Category = "Arc";
}
