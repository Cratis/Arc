// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.AuthorizationAttributeAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_AuthorizationAttributeAnalyzer.for_ARC0021;

public class when_a_controller_names_a_policy
{
    [Fact] async Task should_leave_it_to_mvc_which_evaluates_policies() => await VerifyCS.VerifyAnalyzerAsync(@"
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
    [Microsoft.AspNetCore.Authorization.Authorize(""Auditors"")]
    public class AccountsController { }
}");
}
