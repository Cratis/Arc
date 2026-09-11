// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Verify = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.CommandOperationAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_CommandOperationAnalyzer;

public class when_an_operation_is_file_local
{
    [Fact] public async Task should_report_the_inaccessible_declaration() => await Verify.VerifyAnalyzerAsync(
        "using Cratis.Arc.Commands; file record {|#0:LocalOperation|} : ICommandOperation { public void Execute() { } }",
        Verify.Diagnostic("ARC0018").WithLocation(0));
}
