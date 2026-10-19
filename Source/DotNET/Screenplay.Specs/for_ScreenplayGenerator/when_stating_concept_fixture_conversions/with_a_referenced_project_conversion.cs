// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_stating_concept_fixture_conversions;

public class with_a_referenced_project_conversion : given.a_referenced_concept_fixture
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void should_prove_forwarding_from_the_analyzed_contract_project(bool collection, bool metadataReference)
    {
        var contracts = Analyzed.Project("Library.Contracts", [], ("Contracts/ApprovalLabel.cs", Concept));
        var reference = metadataReference ? Analyzed.Package("Library.Contracts", Concept) : contracts.ToMetadataReference();
        var application = Application(reference, collection);
        Result = new ScreenplayGenerator().Generate([application, contracts], new());
        AssertStated(collection);
    }

    [Theory]
    [InlineData("=> new(value.ToUpperInvariant())")]
    [InlineData("=> new(value)")]
    public void should_still_refuse_transformations_in_a_referenced_project(string conversion)
    {
        var source = Concept.Replace("=> new(value)", conversion, StringComparison.Ordinal);
        if (conversion == "=> new(value)")
        {
            source = source.Replace("ConceptAs<string>(Value)", "ConceptAs<string>(Value.ToUpperInvariant())", StringComparison.Ordinal);
        }
        var contracts = Analyzed.Project("Library.Contracts", [], ("Contracts/ApprovalLabel.cs", source));
        Result = new ScreenplayGenerator().Generate([Application(contracts.ToMetadataReference()), contracts], new());
        AssertRefused();
    }
}
