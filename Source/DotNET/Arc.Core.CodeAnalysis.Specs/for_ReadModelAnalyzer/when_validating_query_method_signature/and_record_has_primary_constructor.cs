// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ReadModelAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ReadModelAnalyzer.when_validating_query_method_signature;

public class and_record_has_primary_constructor : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Queries.ModelBound;
using System.Collections.Generic;

namespace TestNamespace
{
    [ReadModel]
    public record TestReadModel(int Id, string Name)
    {
        public static TestReadModel GetById(int id) => new(id, ""test"");
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
