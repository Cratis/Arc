// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.CommandAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_CommandAnalyzer.when_record_command_missing_attribute;

public class with_primary_constructor : Specification
{
    Task _result;

    void Because() => _result = VerifyCS.VerifyAnalyzerAsync(@"
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
            .WithArguments("TestCommand"));

    [Fact] void should_report_diagnostic() => _result.Wait();
}
