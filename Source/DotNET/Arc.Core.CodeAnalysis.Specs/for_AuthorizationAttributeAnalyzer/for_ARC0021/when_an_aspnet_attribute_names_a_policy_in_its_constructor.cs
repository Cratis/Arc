// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.AuthorizationAttributeAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_AuthorizationAttributeAnalyzer.for_ARC0021;

public class when_an_aspnet_attribute_names_a_policy_in_its_constructor
{
    [Fact] async Task should_report_that_the_policy_is_not_evaluated() => await VerifyCS.VerifyAnalyzerAsync(@"
namespace Microsoft.AspNetCore.Authorization
{
    public class AuthorizeAttribute : System.Attribute
    {
        public AuthorizeAttribute() { }
        public AuthorizeAttribute(string policy) { Policy = policy; }
        public string Policy { get; set; }
    }
}

namespace Cratis.Arc.Authorization
{
    public class AspNetAnonymousEvaluator { }
}

namespace TestNamespace
{
    using System.Collections.Generic;
    using Cratis.Arc.Queries.ModelBound;

    [ReadModel]
    public record Account(string Name)
    {
        [{|#0:Microsoft.AspNetCore.Authorization.Authorize(""Auditors"")|}]
        public static IEnumerable<Account> All() => null;
    }
}",
        VerifyCS.Diagnostic("ARC0021")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("Authorize", "All", "Policy"));
}
