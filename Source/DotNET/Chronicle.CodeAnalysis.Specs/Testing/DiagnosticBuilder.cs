// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;

public class DiagnosticBuilder(string diagnosticId)
{
    readonly string _diagnosticId = diagnosticId;
    DiagnosticSeverity _severity = DiagnosticSeverity.Error;
    readonly List<string> _arguments = [];

    public DiagnosticBuilder WithSeverity(DiagnosticSeverity severity)
    {
        _severity = severity;
        return this;
    }

    public DiagnosticBuilder WithLocation(int location)
    {
        // Location is handled by markers in the source, so we ignore this
        return this;
    }

    public DiagnosticBuilder WithArguments(params string[] arguments)
    {
        _arguments.AddRange(arguments);
        return this;
    }

    public static implicit operator ExpectedDiagnostic(DiagnosticBuilder builder)
    {
        return new ExpectedDiagnostic(builder._diagnosticId, builder._severity, builder._arguments);
    }
}
