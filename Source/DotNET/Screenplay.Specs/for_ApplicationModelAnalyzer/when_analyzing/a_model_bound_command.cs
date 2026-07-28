// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// The input of a command is what the record itself declares. The parameters of its handler are infrastructure -
/// here a read model carrying current state - and are never something a caller sends.
/// </summary>
public class a_model_bound_command : Specification
{
    const string Source = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered(string Name);

        /// <summary>
        /// Registers a new author.
        /// </summary>
        [Command]
        public record RegisterAuthor(string Name, int Age)
        {
            public AuthorRegistered Handle(AuthorRegistered current) => new(Name);
        }
        """;

    ApplicationModelAnalysis _analysis;
    CommandModel _command;

    void Establish()
    {
        _analysis = Analyzed.Source(("Library/Authors/Registration/Registration.cs", Source));
        _command = _analysis.Slice().Commands.First();
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Authors/Registration/Registration.cs", Source)).ShouldBeEmpty();
    [Fact] void should_recover_one_slice() => _analysis.Model.Slices.Count().ShouldEqual(1);
    [Fact] void should_name_the_slice_after_the_last_namespace_segment() => _analysis.Slice().Name.ShouldEqual("Registration");
    [Fact] void should_recover_the_namespace() => _analysis.Slice().Namespace.ShouldEqual("Library.Authors.Registration");
    [Fact] void should_infer_a_state_change_slice() => _analysis.Slice().Kind.ShouldEqual(SliceKind.StateChange);
    [Fact] void should_name_the_command() => _command.Name.ShouldEqual("RegisterAuthor");
    [Fact] void should_take_the_description_from_the_documentation() => _command.Description.ShouldEqual("Registers a new author.");
    [Fact] void should_recover_only_the_declared_properties() => _command.Properties.Select(_ => _.Name).ShouldContainOnly(["Name", "Age"]);
    [Fact] void should_resolve_the_property_types() => _command.Properties.Select(_ => _.Type.Name).ShouldContainOnly(["String", "Int"]);
    [Fact] void should_recover_the_real_file_path() => _command.SourceFilePath.ShouldEqual("Authors/Registration/Registration.cs");
    [Fact] void should_produce_the_event_the_body_constructs() => _command.Produces.Single().EventName.ShouldEqual("AuthorRegistered");
    [Fact] void should_map_the_event_property_from_the_constructor_argument() => _command.Produces.Single().Mappings.Single().ShouldEqual(new PropertyMappingModel("Name", new PropertyPathSource("Name")));
    [Fact] void should_produce_it_unconditionally() => _command.Produces.Single().When.ShouldBeNull();
    [Fact] void should_declare_the_event() => _analysis.Slice().Events.Single().Name.ShouldEqual("AuthorRegistered");
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
