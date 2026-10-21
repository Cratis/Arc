// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators;

/// <summary>
/// Defines diagnostics shared by Arc generators and query tooling.
/// </summary>
internal static class ArcGeneratorDiagnostics
{
    /// <summary>
    /// Reports an invalid explicit query declaration.
    /// </summary>
    internal static readonly DiagnosticDescriptor InvalidDeclaration = new(
        "ARC0026", "Invalid explicit query declaration", "Query '{0}': {1}", "Arc", DiagnosticSeverity.Error, true);

    /// <summary>
    /// Explains when a declared composite key needs manual accessor coverage in strict mode.
    /// </summary>
    internal static readonly DiagnosticDescriptor AccessorMissing = new(
        "ARC0030",
        "Composite key requires accessor registration",
        "Composite key type '{0}' cannot receive complete generated accessors; register it with ProtectedReadModelKeyAccessors.Register<T>() for strict mode",
        "Arc",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    /// <summary>
    /// Explains when a command's key properties cannot be generated and read protection reads them through reflection.
    /// </summary>
    internal static readonly DiagnosticDescriptor CommandKeysNotGenerated = new(
        "ARC0031",
        "Command key properties are read through reflection",
        "Command '{0}' cannot be named from generated code; read protection uses reflection to read its key properties",
        "Arc",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);
}
