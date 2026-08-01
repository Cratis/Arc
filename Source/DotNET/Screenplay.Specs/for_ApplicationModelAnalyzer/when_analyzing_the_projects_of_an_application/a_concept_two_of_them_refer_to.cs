// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing_the_projects_of_an_application;

/// <summary>
/// A concept is declared once at the top of the document and referred to by name from there on, which is a fact
/// about the document rather than about a project. A shared kernel referred to from every project of an application
/// is the ordinary case, and declaring it once per project would leave a document declaring the same concept three
/// times - and the rules a validator states for it attached to whichever copy happened to be first.
/// </summary>
public class a_concept_two_of_them_refer_to : Specification
{
    const string Kernel = """
        using Cratis.Arc.Validation;
        using Cratis.Concepts;
        using FluentValidation;

        namespace Library;

        public record Isbn(string Value) : ConceptAs<string>(Value);

        public class IsbnValidator : ConceptValidator<Isbn>
        {
            public IsbnValidator()
            {
                RuleFor(_ => _.Value).NotEmpty();
            }
        }
        """;

    const string Reserving = """
        using Cratis.Chronicle.Events;

        namespace Library.Lending.Reserving;

        [EventType]
        public record BookReserved(Isbn Isbn);
        """;

    const string Returning = """
        using Cratis.Chronicle.Events;

        namespace Library.Lending.Returning;

        [EventType]
        public record BookReturned(Isbn Isbn);
        """;

    Compilation _kernel;
    Compilation _lending;

    ApplicationModelAnalysis _analysis;

    void Establish()
    {
        _kernel = Analyzed.Project(
            "Library.Kernel",
            [],
            ("Source/Library.Kernel/Kernel.cs", "namespace Library.Kernel;"),
            ("Source/Library.Kernel/Isbn.cs", Kernel));

        _lending = Analyzed.Project(
            "Library.Lending",
            [_kernel.ToMetadataReference()],
            ("Source/Library.Lending/Lending.cs", "namespace Library.Lending;"),
            ("Source/Library.Lending/Reserving/Reserving.cs", Reserving),
            ("Source/Library.Lending/Returning/Returning.cs", Returning));
    }

    void Because() => _analysis = Analyzed.Projects(_lending, _kernel);

    [Fact] void should_compile_the_kernel_project() => Analyzed.ErrorsIn(_kernel).ShouldBeEmpty();
    [Fact] void should_compile_the_lending_project() => Analyzed.ErrorsIn(_lending).ShouldBeEmpty();
    [Fact] void should_declare_the_concept_once() => _analysis.Model.Concepts.Count(_ => string.Equals(_.Name, "Isbn", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_attach_the_rules_the_project_declaring_it_states() => _analysis.Model.Concepts.Single(_ => string.Equals(_.Name, "Isbn", StringComparison.Ordinal)).Validations.ShouldNotBeEmpty();
    [Fact] void should_not_report_it_as_sharing_its_name() => _analysis.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.AmbiguousConceptName).ShouldBeFalse();
}
