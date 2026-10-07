// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_authoring_only_constructs;

public class operation_dependencies : an_authoring_document
{
    void Because() => Generate("""
        using System;
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;
        using Cratis.Execution;
        namespace Library.Authors.Registration;
        public interface IMailer;
        public record AuthorName(string Value) : ConceptAs<string>(Value);
        [ReadModel] public record AuthorState(AuthorName Name);
        [EventType] public record AuthorRegistered(string Name);
        public record SendExternal(string Name) : ICommandOperation
        {
            public void Execute(IMailer mailer, ICorrelationIdAccessor correlation) { }
        }
        public record OnlyFramework(string Name) : ICommandOperation
        {
            public void Execute(ICorrelationIdAccessor correlation) { }
        }
        public record NotifyState(AuthorState State) : ICommandOperation
        {
            public void Execute(IMailer mailer) { }
        }
        public record BatchItem(string Name) : ICommandOperation
        {
            public void Execute(IMailer mailer) { }
        }
        [Command] public record Register(string Name)
        {
            public (AuthorRegistered, SendExternal) Handle() => (new(Name), new SendExternal(Name));
        }
        [Command] public record Correlate(string Name)
        {
            public (AuthorRegistered, OnlyFramework) Handle() => (new(Name), new OnlyFramework(Name));
        }
        [Command] public record Notify([Cratis.Chronicle.Keys.Key] Guid Id, string Name)
        {
            public (AuthorRegistered, NotifyState) Handle(AuthorState state) => (new(Name), new NotifyState(state));
        }
        [Command] public record Batch(string Name)
        {
            public (AuthorRegistered, CommandOperations) Handle(CommandOperations pending) => (new(Name), [new BatchItem(Name), ..pending]);
        }
        """);

    [Fact] void should_not_count_framework_infrastructure_as_an_external_system() => Result.Source.ShouldContain("produces operation SendExternal");
    [Fact] void should_not_declare_a_correlation_accessor_system() => Result.Source.Contains("system CorrelationIdAccessor", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_omit_an_operation_with_only_framework_dependencies() => Result.Source.Contains("produces operation OnlyFramework", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_report_the_operation_with_no_system() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandOperation && diagnostic.Location == "Library.Authors.Registration.Correlate").Message.ShouldContain("uses 0 external systems");
    [Fact] void should_reject_a_whole_read_alias_before_emission() => Result.Model.Slices.SelectMany(slice => slice.Commands).Single(command => command.Name == "Notify").Authoring!.Operations.ShouldBeEmpty();
    [Fact] void should_report_the_unreadable_whole_read_input() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandOperation && diagnostic.Location == "Library.Authors.Registration.Notify").Message.ShouldContain("input not readable");
    [Fact] void should_keep_the_literal_member_of_a_batch_with_a_spread() => Result.Source.ShouldContain("produces operation BatchItem");
    [Fact] void should_report_the_batch_spread_for_its_command() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandOperation && diagnostic.Location == "Library.Authors.Registration.Batch").Message.ShouldContain("spread whose contents cannot be read");
    [Fact] void should_register_the_read_concept_during_analysis() => Result.Model.Concepts.Select(concept => concept.Name).ShouldContain("AuthorName");
    [Fact] void should_keep_the_read_concept_for_the_read_model_holding_it_after_dropping_the_read() => Result.Source.Split('\n').Select(line => line.Trim()).ShouldContain("readmodel AuthorState");
    [Fact] void should_type_the_read_model_by_the_concept_it_keeps() => Result.Source.Split('\n').Select(line => line.Trim()).ShouldContain("name AuthorName");
    [Fact] void should_analyze_valid_source() => Compiled.Success.ShouldBeTrue();
}
