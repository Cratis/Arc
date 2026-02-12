// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.CommandAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_CommandAnalyzer.when_validating_command_missing_attribute;

public class and_record_has_primary_constructor : Specification
{
    Exception result;

    async Task Because()
    {
        try
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
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
        }
        catch (Exception ex)
        {
            result = ex;
        }
    }

    [Fact] void should_report_diagnostic() => result.ShouldBeNull();
}
