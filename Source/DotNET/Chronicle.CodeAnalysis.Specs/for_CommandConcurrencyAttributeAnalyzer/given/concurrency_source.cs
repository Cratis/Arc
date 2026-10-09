// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandConcurrencyAttributeAnalyzer.given;

public static class concurrency_source
{
    public const string Preamble = @"
        using System;
        using System.Threading.Tasks;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.EventSequences;
        using Cratis.Chronicle.EventSources;
        public class EventSourceAttribute<T> : Attribute;
        [EventType] public record E;
        [EventSource(Concurrency = ConcurrencyDimensions.EventSourceId)] public class Definition : IEventSource;
        ";

    public static ExpectedDiagnostic Warning(string attribute = "EventStreamId", string command = "C") =>
        new("ARCCHR0016", DiagnosticSeverity.Warning, command, attribute, "concurrency: true");

    public static string Command(string attributes, string signature, string body = "=> default!;") =>
        Preamble + "\n[Command]\n" + attributes + "\npublic record C { public " + signature + " Handle() " + body + " }";
}
