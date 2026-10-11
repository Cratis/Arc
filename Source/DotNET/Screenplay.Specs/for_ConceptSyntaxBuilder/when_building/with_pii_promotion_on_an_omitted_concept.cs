// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ConceptSyntaxBuilder.when_building;

public class with_pii_promotion_on_an_omitted_concept : given.a_concept_syntax_builder
{
    IEnumerable<ConceptSyntax> _result;

    void Because() => _result = _builder.Build(
    [
        new ConceptModel("MembershipTier", ScreenplayPrimitive.Enum, true, [], [])
        {
            UnmarkedPiiUses = ["Library.Member.Tier"]
        }
    ]);

    [Fact] void should_omit_the_concept() => _result.ShouldBeEmpty();
    [Fact] void should_not_claim_the_document_promotes_personal_data() => _diagnostics.All.Select(diagnostic => diagnostic.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.EnumWithoutValues]);
}
