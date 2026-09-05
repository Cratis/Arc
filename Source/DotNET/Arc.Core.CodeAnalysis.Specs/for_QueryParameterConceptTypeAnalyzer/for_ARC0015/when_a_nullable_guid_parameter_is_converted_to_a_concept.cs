// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.QueryParameterConceptTypeAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_QueryParameterConceptTypeAnalyzer.for_ARC0015;

/// <summary>
/// Declaring the parameter optional is orthogonal to declaring it raw, so a nullable backing type is the same
/// mistake and has to be unwrapped before the check rather than skipped.
/// </summary>
public class when_a_nullable_guid_parameter_is_converted_to_a_concept
{
    [Fact] async Task should_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.Collections.Generic;
using Cratis.Concepts;
using Cratis.Arc.Queries.ModelBound;

namespace TestNamespace
{
    public record RequestId(Guid Value) : ConceptAs<Guid>(Value);

    [ReadModel]
    public record Request(RequestId Id)
    {
        public static IEnumerable<Request> ById({|#0:Guid? id|}) => Find((RequestId)id.Value);
        static IEnumerable<Request> Find(RequestId id) => null;
    }
}",
        VerifyCS.Diagnostic("ARC0015")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("id", "System.Guid?", "RequestId"));
}
