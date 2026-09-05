// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ReadModelAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ReadModelAnalyzer;

public class when_readmodel_has_a_generic_query_shaped_method
{
    [Fact] async Task should_report_diagnostic_for_an_internal_generic_helper() => await VerifyCS.VerifyAnalyzerAsync(@"
using System.Collections.Generic;
using Cratis.Arc.Queries.ModelBound;

namespace TestNamespace
{
    [ReadModel]
    public class TestReadModel
    {
        internal static {|#0:IEnumerable<TestReadModel> CountOf<TDocument>(IEnumerable<TDocument> source)|} => null;
    }
}",
        VerifyCS.Diagnostic("ARC0014")
            .WithLocation(0)
            .WithArguments("CountOf", "TestReadModel"));

    [Fact] async Task should_not_report_diagnostic_for_a_non_generic_query() => await VerifyCS.VerifyAnalyzerAsync(@"
using System.Collections.Generic;
using Cratis.Arc.Queries.ModelBound;

namespace TestNamespace
{
    [ReadModel]
    public class TestReadModel
    {
        public static IEnumerable<TestReadModel> All() => null;
    }
}");
}
