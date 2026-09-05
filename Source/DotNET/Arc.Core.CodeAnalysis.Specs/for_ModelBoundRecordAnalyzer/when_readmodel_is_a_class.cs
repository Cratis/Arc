// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ModelBoundRecordAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ModelBoundRecordAnalyzer;

public class when_readmodel_is_a_class
{
    [Fact] async Task should_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Queries.ModelBound;

namespace TestNamespace
{
    [ReadModel]
    public class {|#0:AuthorDetails|}
    {
        public string Name { get; set; }
    }
}",
        VerifyCS.Diagnostic("ARC0008")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("AuthorDetails"));
}
