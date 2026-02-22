// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.CommandAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_CommandAnalyzer;

public class when_command_like_type_missing_attribute
{
    [Fact] async Task should_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
namespace TestNamespace
{
    public class {|#0:TestCommand|}
    {
        public string Name { get; set; }
        
        public void Handle()
        {
        }
    }
}",
        VerifyCS.Diagnostic("ARC0002")
            .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("TestCommand"));
}
