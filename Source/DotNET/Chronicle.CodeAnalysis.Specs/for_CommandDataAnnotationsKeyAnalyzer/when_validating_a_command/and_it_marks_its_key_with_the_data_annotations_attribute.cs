// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandDataAnnotationsKeyAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandDataAnnotationsKeyAnalyzer.when_validating_a_command;

public class and_it_marks_its_key_with_the_data_annotations_attribute : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    public record CustomerRenamed(string Name);

    [Command]
    public record RenameCustomer([property: Key] Guid {|#0:CustomerId|}, string NewName)
    {
        public CustomerRenamed Handle() => new(NewName);
    }
}",
                VerifyCS.Diagnostic("ARCCHR0008")
                    .WithSeverity(DiagnosticSeverity.Warning)
                    .WithLocation(0)
                    .WithArguments("RenameCustomer", "CustomerId")));

    [Fact] void should_report_diagnostic() => _result.ShouldBeNull();
}
