// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

public class unreferenced_local_and_dependency_concepts : Specification
{
    const string PackageSource = """
        using Cratis.Concepts;
        namespace Dependency;
        public record ExternalConcept(string Value) : ConceptAs<string>(Value);
        public enum ExternalEnumeration { First, Second }
        """;

    const string Source = """
        using Cratis.Concepts;
        namespace Library;
        public record LocalConcept(string Value) : ConceptAs<string>(Value);
        """;

    MetadataReference _package;
    ApplicationModelAnalysis _analysis;

    void Establish() => _package = Analyzed.Package("Dependency", PackageSource);
    void Because() => _analysis = Analyzed.SourceReferencing(_package, (Analyzed.SlicePath, Source));

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_package, (Analyzed.SlicePath, Source)).ShouldBeEmpty();
    [Fact] void should_recover_the_local_declaration_without_any_executable_artifacts() => _analysis.Model.Concepts.Select(concept => concept.Name).ShouldContainOnly(["LocalConcept"]);
    [Fact] void should_not_recover_unused_concepts_from_the_dependency() => _analysis.Model.Concepts.Any(concept => concept.Name == "ExternalConcept").ShouldBeFalse();
    [Fact] void should_not_recover_unused_enumerations_from_the_dependency() => _analysis.Model.Concepts.Any(concept => concept.Name == "ExternalEnumeration").ShouldBeFalse();
}
