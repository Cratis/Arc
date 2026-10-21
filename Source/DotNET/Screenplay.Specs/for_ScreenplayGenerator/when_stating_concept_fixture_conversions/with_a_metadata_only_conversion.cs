// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_stating_concept_fixture_conversions;

public class with_a_metadata_only_conversion : given.a_referenced_concept_fixture
{
    [Theory]
    [InlineData("Cratis.Contracts", false)]
    [InlineData("Cratis.Contracts", true)]
    [InlineData("Library.Contracts", false)]
    [InlineData("Library.Contracts", true)]
    public void should_trust_only_framework_assemblies_without_source(string assembly, bool collection)
    {
        var reference = Analyzed.Package(assembly, Concept);
        Result = new ScreenplayGenerator().Generate(Application(reference, collection), new());
        if (assembly.StartsWith("Cratis.", StringComparison.Ordinal))
        {
            AssertStated(collection);
        }
        else
        {
            AssertRefused();
        }
    }
}
