// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventSourceIdAnalyzer.when_validating_raw_guid_response.given;

public static class response_source
{
    public const string Preamble = @"
        using System;
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.EventSequences;
        using Cratis.Chronicle.Keys;
        using Cratis.Monads;
        using OneOf;
        [EventType] public record E;
        public record Failure;
        ";

    public static ExpectedDiagnostic Warning(string command = "C") => new("ARCCHR0010", DiagnosticSeverity.Warning, command, "E", "If this Guid is intended to identify the event source");

    public static string Command(string signature, string body) => Preamble + "\n[Command] public record C { public {|#0:" + signature + "|} Handle() " + body + " }";
}
