// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventSourceIdAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventSourceIdAnalyzer.when_validating_raw_guid_response;

public class and_keyless_command_returns_raw_guid_without_an_event : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    public record Receipt(string Number);

    [Command]
    public record CreateReceipt()
    {
        public (Guid, Receipt) Handle() => (Guid.NewGuid(), new Receipt(""A-1""));
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
