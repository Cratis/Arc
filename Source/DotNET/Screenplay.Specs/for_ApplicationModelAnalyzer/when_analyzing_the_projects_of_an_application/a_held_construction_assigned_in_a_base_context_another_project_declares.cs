// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing_the_projects_of_an_application;

/// <summary>
/// A command put together in a base context another project declares is held by a field the consumer sees through
/// references of its own, so the field the scenario names is not the symbol the assignment was recorded under.
/// </summary>
public class a_held_construction_assigned_in_a_base_context_another_project_declares : Specification
{
    const string Slice = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered(string Name);

        [Command]
        public record RegisterAuthor(string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """;

    const string Context = """
        using Cratis.Specifications;
        using Library.Authors.Registration;

        namespace Library.Authors.Registration;

        public class a_registration : Specification
        {
            protected RegisterAuthor _command = null!;

            void Establish() => _command = new RegisterAuthor("Jane Austen");
        }
        """;

    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Authors.Registration;
        using Xunit;

        namespace Library.Authors.Registration.when_registering;

        public class and_the_name_is_already_taken : a_registration
        {
            readonly CommandScenario<RegisterAuthor> _scenario = new();
            Result _result = null!;

            async Task Because() => _result = await _scenario.Execute(_command);

            [Fact] void should_not_succeed() => _result.ShouldHaveValidationErrors();
        }
        """;

    Compilation _application;
    Compilation _contexts;
    Compilation _consumer;
    ApplicationModelAnalysis _analysis;

    void Establish()
    {
        _application = Analyzed.Project("Library", [], ("Library/Authors/Registration/Registration.cs", Slice));
        _contexts = Analyzed.Project(
            "Library.Specifications.Contexts",
            [_application.ToMetadataReference()],
            ("Library.Specifications.Contexts/a_registration.cs", Context));
        _consumer = Analyzed.Project(
            "Library.Specifications",
            [],
            ("Library.Specifications/when_registering/and_the_name_is_already_taken.cs", Scenario),
            (IntegrationTesting.Path, IntegrationTesting.Source))
            .WithReferences(((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator).Select(_ => MetadataReference.CreateFromFile(_))
                .Append<MetadataReference>(_application.ToMetadataReference())
                .Append<MetadataReference>(_contexts.ToMetadataReference()));
    }

    void Because() => _analysis = Analyzed.Projects(_application, _contexts, _consumer);

    SpecificationModel Authored => _analysis.Model.Slices.Single(_ => _.Name == "Registration").Specifications.Single();

    [Fact] void should_compile_the_application() => Analyzed.ErrorsIn(_application).ShouldBeEmpty();
    [Fact] void should_compile_the_contexts() => Analyzed.ErrorsIn(_contexts).ShouldBeEmpty();
    [Fact] void should_compile_the_consumer() => Analyzed.ErrorsIn(_consumer).ShouldBeEmpty();
    [Fact] void should_compare_distinct_held_fields() => SymbolEqualityComparer.Default.Equals(
        _contexts.GetTypeByMetadataName("Library.Authors.Registration.a_registration")!.GetMembers("_command").Single(),
        _consumer.GetTypeByMetadataName("Library.Authors.Registration.a_registration")!.GetMembers("_command").Single()).ShouldBeFalse();
    [Fact] void should_read_the_command_through_the_inherited_member() => Authored.When.Name.ShouldEqual("RegisterAuthor");
    [Fact] void should_state_the_values_the_command_was_put_together_with() => Authored.When.Values.ShouldContainOnly([new PropertyMappingModel("Name", new LiteralSource("Jane Austen"))]);
}
