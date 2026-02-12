// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.CommandAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_CommandAnalyzer.when_command_is_record_type;

public class with_primary_constructor : Specification
{
    Task _result;

    void Because() => _result = VerifyCS.VerifyAnalyzerAsync(@"
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

    [Fact] void should_not_report_diagnostic() => _result.Wait();
}
