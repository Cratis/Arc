// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.CodeAnalysis;

/// <summary>
/// Diagnostic descriptors for Arc analyzers.
/// </summary>
static class DiagnosticDescriptors
{
    const string Category = "Arc";

    /// <summary>
    /// ARC001: Incorrect Command handler signature.
    /// </summary>
    public static readonly DiagnosticDescriptor ARC001_IncorrectCommandHandlerSignature = new(
        id: "ARC001",
        title: "Incorrect Command handler signature",
        messageFormat: "Command type '{0}' must have a Handle method with void or Task return type (or Task<T> for a result). Found: {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Command types with [Command] attribute must have a Handle method that returns void, Task, or Task<TResult>.");

    /// <summary>
    /// ARC002: Incorrect Query method signature on ReadModel.
    /// </summary>
    public static readonly DiagnosticDescriptor ARC002_IncorrectQueryMethodSignature = new(
        id: "ARC002",
        title: "Incorrect Query method signature on ReadModel",
        messageFormat: "Query method '{0}' on ReadModel '{1}' must return the ReadModel type, a collection of it, Task<ReadModel>, Task<IEnumerable<ReadModel>>, IAsyncEnumerable<ReadModel>, or ISubject<ReadModel>. Found: {2}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Query methods on types with [ReadModel] attribute must return the ReadModel type, a collection, Task, IAsyncEnumerable, or ISubject of the ReadModel type.");

    /// <summary>
    /// ARC003: Missing [Command] attribute on command-like type.
    /// </summary>
    public static readonly DiagnosticDescriptor ARC003_MissingCommandAttribute = new(
        id: "ARC003",
        title: "Missing [Command] attribute on command-like type",
        messageFormat: "Type '{0}' has a Handle method but is missing the [Command] attribute",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Types with Handle methods should have the [Command] attribute to be recognized as commands.");

    /// <summary>
    /// ARC004: Missing [ReadModel] attribute on query-like type.
    /// </summary>
    public static readonly DiagnosticDescriptor ARC004_MissingReadModelAttribute = new(
        id: "ARC004",
        title: "Missing [ReadModel] attribute on query-like type",
        messageFormat: "Type '{0}' appears to be a ReadModel with query methods but is missing the [ReadModel] attribute",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Types that appear to be ReadModels with query methods should have the [ReadModel] attribute.");
}
