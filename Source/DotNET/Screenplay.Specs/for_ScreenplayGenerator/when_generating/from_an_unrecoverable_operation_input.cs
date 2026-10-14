// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_unrecoverable_operation_input : an_unrecoverable_generated_document
{
    void Because() => GenerateAcrossOptions(WithUnavailable("""
        public interface IMailer { System.Threading.Tasks.Task Send(string name); }
        public record CorrelateAuthor(AuthorId Id) : ICommandOperation
        {
            public System.Threading.Tasks.Task Execute(IMailer mailer) => mailer.Send(Id.ToString());
        }
        public record WelcomeAuthor(string Name) : ICommandOperation
        {
            public System.Threading.Tasks.Task Execute(IMailer mailer) => mailer.Send(Name);
        }
        [Command] public record RegisterAuthor(string Name)
        {
            public (AuthorRegistered, CorrelateAuthor, WelcomeAuthor) Handle([GeneratedValue] AuthorId authorId)
            {
                var alias = authorId;
                return (new(Name), new(alias), new(Name));
            }
        }
        """).Replace("using System;", "using System;\nusing Cratis.Arc.Commands;", StringComparison.Ordinal));

    [Fact] void should_withhold_the_whole_dependent_operation() => Documents.ShouldEachConformTo(document => !document.Result.Source.Contains("produces operation CorrelateAuthor", StringComparison.Ordinal));
    [Fact] void should_keep_independent_operations_in_authoring_output() => Documents.Where(document => document.Options.AuthoringOnlyConstructs).ShouldEachConformTo(document => document.Result.Source.Contains("produces operation WelcomeAuthor", StringComparison.Ordinal));
    [Fact] void should_keep_the_independent_event() => Documents.ShouldEachConformTo(document => document.Result.Source.Contains("produces AuthorRegistered", StringComparison.Ordinal));
    [Fact] void should_report_the_dependent_input() => Documents.Where(document => document.Options.AuthoringOnlyConstructs).ShouldEachConformTo(document => document.Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandOperation).Message.Contains("unavailable generated input 'authorId'", StringComparison.Ordinal));
    [Fact] void should_bind_and_round_trip_without_unexpected_errors() => AssertFaithfulDocuments();
}
