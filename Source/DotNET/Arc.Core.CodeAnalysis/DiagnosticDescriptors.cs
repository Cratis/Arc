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

    const string Category = "Arc";
}
