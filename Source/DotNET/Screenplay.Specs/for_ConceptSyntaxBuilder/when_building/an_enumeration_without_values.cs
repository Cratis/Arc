// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ConceptSyntaxBuilder.when_building;

/// <summary>
/// An enumeration declaration whose body is empty says nothing and does not compile, so it is left out - visibly.
/// </summary>
public class an_enumeration_without_values : given.a_concept_syntax_builder
{
    IEnumerable<ConceptSyntax> _result;

    void Because() => _result = _builder.Build([new ConceptModel("MembershipTier", ScreenplayPrimitive.Enum, false, [], [])]);

    [Fact] void should_declare_nothing() => _result.ShouldBeEmpty();
    [Fact] void should_report_the_concept() => _diagnostics.All.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.EnumWithoutValues]);
}
