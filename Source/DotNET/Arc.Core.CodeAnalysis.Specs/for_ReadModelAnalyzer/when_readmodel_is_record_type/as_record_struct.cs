// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ReadModelAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ReadModelAnalyzer.when_readmodel_is_record_type;

public class as_record_struct : Specification
{
    Task _result;

    void Because() => _result = VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Queries.ModelBound;
using System.Collections.Generic;

namespace TestNamespace
{
    [ReadModel]
    public record struct TestReadModel(int Id, string Name)
    {
        public static TestReadModel GetById(int id) => new(id, ""test"");
    }
}");

    [Fact] void should_not_report_diagnostic() => _result.Wait();
}
