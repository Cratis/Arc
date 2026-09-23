// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.AuthorizationAttributeAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_AuthorizationAttributeAnalyzer.for_ARC0019;

public class when_allow_anonymous_and_roles_are_on_one_query_method
{
    [Fact] async Task should_report_the_conflict_naming_roles() => await VerifyCS.VerifyAnalyzerAsync(@"
using System.Collections.Generic;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;

namespace TestNamespace
{
    [ReadModel]
    public record Account(string Name)
    {
        [Roles(""Admin"")]
        [{|#0:AllowAnonymous|}]
        public static IEnumerable<Account> All() => null;
    }
}",
        VerifyCS.Diagnostic("ARC0019")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("All", "Roles"));
}
