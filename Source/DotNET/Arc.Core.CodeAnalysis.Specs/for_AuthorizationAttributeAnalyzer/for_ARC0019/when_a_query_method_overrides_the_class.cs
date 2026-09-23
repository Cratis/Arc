// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.AuthorizationAttributeAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_AuthorizationAttributeAnalyzer.for_ARC0019;

public class when_a_query_method_overrides_the_class
{
    [Fact] async Task should_not_report_an_override_across_declarations() => await VerifyCS.VerifyAnalyzerAsync(@"
using System.Collections.Generic;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;

namespace TestNamespace
{
    [ReadModel]
    [Authorize]
    public record Account(string Name)
    {
        [AllowAnonymous]
        public static IEnumerable<Account> Public() => null;
    }
}");
}
