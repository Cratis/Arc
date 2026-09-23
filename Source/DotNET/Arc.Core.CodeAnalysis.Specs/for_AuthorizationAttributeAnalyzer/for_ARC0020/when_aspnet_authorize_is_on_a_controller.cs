// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.AuthorizationAttributeAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_AuthorizationAttributeAnalyzer.for_ARC0020;

public class when_aspnet_authorize_is_on_a_controller
{
    [Fact] async Task should_not_report_where_mvc_enforces_it() => await VerifyCS.VerifyAnalyzerAsync(@"
namespace Microsoft.AspNetCore.Authorization
{
    public class AuthorizeAttribute : System.Attribute { }
    public class AllowAnonymousAttribute : System.Attribute { }
}

namespace TestNamespace
{
    using Microsoft.AspNetCore.Authorization;

    [Authorize]
    public class AccountsController { }
}");
}
