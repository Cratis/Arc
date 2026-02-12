// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.CommandAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_CommandAnalyzer.when_validating_command_attribute;

public class and_record_has_primary_constructor : Specification
{
    async Task Because() => await VerifyCS.VerifyAnalyzerAsync(@"
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
}");

    [Fact] void should_not_report_diagnostic() { }
}
