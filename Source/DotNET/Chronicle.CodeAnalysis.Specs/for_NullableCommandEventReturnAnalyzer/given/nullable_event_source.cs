// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_NullableCommandEventReturnAnalyzer.given;

public static class nullable_event_source
{
    public const string Preamble = @"
        using System;
        using System.Threading.Tasks;
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Validation;
        using Cratis.Chronicle.Events;
        using Cratis.Monads;
        using OneOf;
        [EventType] public record E;
        public record Failure;
        public record Operation : ICommandOperation;
        ";

    public static ExpectedDiagnostic Warning(string command = "C", string eventName = "E") =>
        new("ARCCHR0015", DiagnosticSeverity.Warning, command, eventName, "Result<TEvent, ValidationResult>", "ValidationResult.Error(...)", "validator or Provide()");

    public static string Command(string signature, string body, string modifiers = "") =>
        Preamble + "\n[Command] public record C { public " + modifiers + "{|#0:" + signature + "|} Handle() " + body + " }";
}
