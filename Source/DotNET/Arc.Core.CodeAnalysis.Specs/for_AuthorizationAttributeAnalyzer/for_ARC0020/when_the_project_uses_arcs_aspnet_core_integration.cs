// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.AuthorizationAttributeAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_AuthorizationAttributeAnalyzer.for_ARC0020;

public class when_the_project_uses_arcs_aspnet_core_integration
{
    [Fact] async Task should_not_report_an_attribute_the_integration_enforces() => await VerifyCS.VerifyAnalyzerAsync(@"
namespace Microsoft.AspNetCore.Authorization
{
    public class AuthorizeAttribute : System.Attribute { }
}

namespace Cratis.Arc.Authorization
{
    public class AspNetAnonymousEvaluator { }
}

namespace TestNamespace
{
    using Cratis.Arc.Commands.ModelBound;

    [Command]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public record CloseAccount(string Id)
    {
        public void Handle() { }
    }
}");
}
