// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.AuthorizationAttributeAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_AuthorizationAttributeAnalyzer.for_ARC0021;

public class when_an_aspnet_attribute_names_a_policy_in_its_constructor
{
    [Fact] async Task should_not_report_a_supported_policy() => await VerifyCS.VerifyAnalyzerAsync(@"
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
        [Microsoft.AspNetCore.Authorization.Authorize(""Auditors"")]
        public static IEnumerable<Account> All() => null;
    }
}");
}
