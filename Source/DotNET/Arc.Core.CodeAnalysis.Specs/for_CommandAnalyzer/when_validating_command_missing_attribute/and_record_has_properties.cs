// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.CommandAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_CommandAnalyzer.when_validating_command_missing_attribute;

public class and_record_has_properties : Specification
{
    async Task Because() => await VerifyCS.VerifyAnalyzerAsync(@"
namespace TestNamespace
{
    public record {|#0:TestCommand|}
    {
        public string Name { get; set; }
        
        public void Handle()
        {
        }
    }
}",
        VerifyCS.Diagnostic("ARC0002")
            .WithLocation(0)
            .WithArguments("TestCommand"));

    [Fact] void should_report_diagnostic() { }
}
