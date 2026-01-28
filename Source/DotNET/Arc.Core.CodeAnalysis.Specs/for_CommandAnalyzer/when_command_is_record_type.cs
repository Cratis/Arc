// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.CommandAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_CommandAnalyzer;

public class when_command_is_record_type
{
    [Fact] void should_not_report_diagnostic_for_record_with_properties() => VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    [Command]
    public record TestCommand
    {
        public string Name { get; set; }
        public int Age { get; set; }
        
        public void Handle()
        {
        }
    }
}").Wait();

    [Fact] void should_not_report_diagnostic_for_record_with_primary_constructor() => VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    [Command]
    public record TestCommand(string Name, int Age)
    {
        public void Handle()
        {
        }
    }
}").Wait();

    [Fact] void should_not_report_diagnostic_for_record_with_mixed_properties() => VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    [Command]
    public record TestCommand(string Name, int Age)
    {
        public string Email { get; set; }
        
        public void Handle()
        {
        }
    }
}").Wait();

    [Fact] void should_not_report_diagnostic_for_record_struct() => VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    [Command]
    public record struct TestCommand(string Name, int Age)
    {
        public void Handle()
        {
        }
    }
}").Wait();
}
