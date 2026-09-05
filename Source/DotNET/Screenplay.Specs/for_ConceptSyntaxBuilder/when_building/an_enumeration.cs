// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ConceptSyntaxBuilder.when_building;

/// <summary>
/// Enumeration values have to match <c>^[a-z_]\w*$</c>, so declared PascalCase members have to be lower camelled.
/// </summary>
public class an_enumeration : given.a_concept_syntax_builder
{
    ConceptSyntax _result;

    void Because() => _result = _builder
        .Build([new ConceptModel("MembershipTier", ScreenplayPrimitive.Enum, false, ["Standard", "Premium"], [])])
        .Single();

    [Fact] void should_declare_it_as_an_enum() => _result.IsEnum.ShouldBeTrue();
    [Fact] void should_lower_camel_every_value() => _result.Values.ShouldEqual(["standard", "premium"]);
    [Fact] void should_report_nothing() => _diagnostics.All.ShouldBeEmpty();
}
