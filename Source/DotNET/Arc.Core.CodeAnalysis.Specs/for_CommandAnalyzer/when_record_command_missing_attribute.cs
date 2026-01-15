// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.CommandAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_CommandAnalyzer;

public class when_record_command_missing_attribute
{
    [Fact] void should_report_diagnostic_for_record_with_properties() => VerifyCS.VerifyAnalyzerAsync(@"
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
            .WithArguments("TestCommand")).Wait();

    [Fact] void should_report_diagnostic_for_record_with_primary_constructor() => VerifyCS.VerifyAnalyzerAsync(@"
namespace TestNamespace
{
    public record {|#0:TestCommand|}(string Name, int Age)
    {
        public void Handle()
        {
        }
    }
}",
        VerifyCS.Diagnostic("ARC0002")
            .WithLocation(0)
            .WithArguments("TestCommand")).Wait();
}
