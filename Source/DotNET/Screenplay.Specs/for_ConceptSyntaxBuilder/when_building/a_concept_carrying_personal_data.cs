// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Concepts;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ConceptSyntaxBuilder.when_building;

/// <summary>
/// The attribute is inherited by every property typed with the concept, so declaring it once at the top of the
/// document is what makes erasure rules readable at a glance.
/// </summary>
public class a_concept_carrying_personal_data : given.a_concept_syntax_builder
{
    ConceptSyntax _marked;
    ConceptSyntax _unmarked;

    void Because()
    {
        var concepts = _builder.Build(
        [
            new ConceptModel("AuthorName", ScreenplayPrimitive.String, true, [], []),
            new ConceptModel("BookTitle", ScreenplayPrimitive.String, false, [], [])
        ]).ToList();

        _marked = concepts.First(_ => _.Name == "AuthorName");
        _unmarked = concepts.First(_ => _.Name == "BookTitle");
    }

    [Fact] void should_mark_the_concept_carrying_personal_data() => _marked.AttributeNames.ShouldContainOnly([ConceptSyntaxBuilder.PersonallyIdentifiableInformation]);
    [Fact] void should_not_mark_a_concept_that_carries_none() => _unmarked.Attributes.ShouldBeEmpty();
}
