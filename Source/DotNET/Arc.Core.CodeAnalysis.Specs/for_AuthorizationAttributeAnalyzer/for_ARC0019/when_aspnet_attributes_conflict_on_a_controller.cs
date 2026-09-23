// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.AuthorizationAttributeAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_AuthorizationAttributeAnalyzer.for_ARC0019;

public class when_aspnet_attributes_conflict_on_a_controller
{
    [Fact] async Task should_leave_an_mvc_controller_to_asp_net_core() => await VerifyCS.VerifyAnalyzerAsync(@"
namespace Microsoft.AspNetCore.Authorization
{
    public class AuthorizeAttribute : System.Attribute { }
    public class AllowAnonymousAttribute : System.Attribute { }
}

namespace TestNamespace
{
    using Microsoft.AspNetCore.Authorization;

    [Authorize]
    [AllowAnonymous]
    public class AccountsController { }
}");
}
