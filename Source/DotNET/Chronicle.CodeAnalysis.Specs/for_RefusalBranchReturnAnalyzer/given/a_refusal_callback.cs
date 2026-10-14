// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_RefusalBranchReturnAnalyzer.given;

public class a_refusal_callback : Specification
{
    protected const string Preamble = @"
        using System;
        using System.Collections.Generic;
        using Cratis.Arc.Chronicle.Reactors;
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.EventSequences;
        [EventType] public record Recorded;
        [Command] public record DoWork { public Recorded Handle() => new(); }
        public record Operation : ICommandOperation;
        ";

    protected static ExpectedDiagnostic Warning => new("ARCCHR0027", DiagnosticSeverity.Warning, "registered events", "Acknowledge");

    protected static string Callback(string expression, string members = "") => Preamble + $@"
        public class Reactor
        {{
            public CommandInvocation InvokeWork() => Invoke.Command(new DoWork()).OnRefused(Refused.Any, {expression});
            {members}
        }}
        ";
}
