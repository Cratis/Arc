// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventStreamIdAnalyzer.given;

public static class stream_id_source
{
    public const string Preamble = @"
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Concepts;
        using Cratis.Chronicle.Events;
        public enum Kind { One }
        public record Period(DateOnly Value) : ConceptAs<DateOnly>(Value);
        public record ScopeId(Guid Value) : EventSourceId<Guid>(Value);
        public record Complex(string Name);
        ";

    public static ExpectedDiagnostic Invalid(string template, string problem) =>
        new("ARCCHR0017", DiagnosticSeverity.Error, "C", template, problem);

    public static string Command(string attribute, string members = "", string bases = "") =>
        Preamble + "\n[Command] " + attribute + " public record C(string Name)" + bases + " { " + members + " public void Handle() { } }";
}
