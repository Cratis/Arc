// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandDataAnnotationsKeyAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandDataAnnotationsKeyAnalyzer.when_validating_a_command;

/// <summary>
/// The data annotations attribute is what an Entity Framework Core read model marks its primary key with, so reporting
/// it anywhere other than on a command would report correct code.
/// </summary>
public class and_the_marked_type_is_not_a_command : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.ComponentModel.DataAnnotations;

namespace TestNamespace
{
    public class Customer
    {
        [Key]
        public Guid Id { get; set; }
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
