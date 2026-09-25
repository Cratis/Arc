// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.AuthorizationAttributeAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_AuthorizationAttributeAnalyzer.for_ARC0021;

public class when_an_arc_attribute_names_a_policy
{
    [Fact] async Task should_not_report_a_supported_policy() => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    [Command]
    [Authorize(Policy = ""ActiveSubscription"")]
    public record UpdateProfile(string Name)
    {
        public void Handle() { }
    }
}");
}
