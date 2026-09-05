// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandDataAnnotationsKeyAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandDataAnnotationsKeyAnalyzer.when_validating_a_command;

public class and_it_marks_nothing : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    public record CustomerRenamed(string Name);

    [Command]
    public record RenameCustomer(Guid CustomerId, string NewName)
    {
        public CustomerRenamed Handle() => new(NewName);
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
