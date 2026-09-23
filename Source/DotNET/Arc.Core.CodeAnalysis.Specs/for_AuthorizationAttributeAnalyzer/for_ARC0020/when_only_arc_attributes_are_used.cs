// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.AuthorizationAttributeAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_AuthorizationAttributeAnalyzer.for_ARC0020;

public class when_only_arc_attributes_are_used
{
    [Fact] async Task should_not_report_anything() => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    [Command]
    [Authorize]
    public record CloseAccount(string Id)
    {
        public void Handle() { }
    }
}");
}
