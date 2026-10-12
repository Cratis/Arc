// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_authoring_only_constructs;

public class an_unrecoverable_generation_strategy : an_authoring_document
{
    void Because() => Generate(GeneratedValueSources.With("""
        public interface IMailer { System.Threading.Tasks.Task Send(string name); }
        public record SendWelcomeEmail(string Recipient) : ICommandOperation
        {
            public System.Threading.Tasks.Task Execute(IMailer mailer) => mailer.Send(Recipient);
        }
        public record CorrelateAuthor(AuthorId Id) : ICommandOperation
        {
            public System.Threading.Tasks.Task Execute(IMailer mailer) => mailer.Send(Id.ToString());
        }
        [Command] public record RegisterAuthor(string Name)
        {
            public (AuthorRegistered, SendWelcomeEmail) Handle([GeneratedValue] AuthorId authorId) => (new(Name), new(Name));
        }
        [Command] public record Correlate(string Name)
        {
            public (AuthorRegistered, CorrelateAuthor) Handle([GeneratedValue] AuthorId authorId) => (new(Name), new(authorId));
        }
        [Command] public record ProvisionAuthor(string Name)
        {
            public void Provide() { }
            public AuthorRegistered Handle([GeneratedValue] AuthorId authorId) => new(Name);
        }
        """).Replace("using System;", "using System;\nusing Cratis.Arc.Commands;", StringComparison.Ordinal)
            .Replace("public static AuthorId New() => new(Guid.NewGuid());", "public static AuthorId New() => new(Guid.Parse(System.Security.Cryptography.RandomNumberGenerator.GetHexString(64, lowercase: true)));", StringComparison.Ordinal));

    [Fact] void should_report_each_unrecoverable_strategy() => Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable).ShouldEqual(3);
    [Fact] void should_read_independent_returned_operations() => Result.Source.ShouldContain("produces operation SendWelcomeEmail");
    [Fact] void should_map_independent_operation_inputs() => Result.Source.ShouldContain("recipient String = name");
    [Fact] void should_report_operations_that_depend_on_unrecovered_generation() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandOperation && diagnostic.Location == "Library.Authors.Registration.Correlate").Message.ShouldContain("input not readable");
    [Fact] void should_report_the_operation_opt_in_in_default_output() => Off.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandOperation && diagnostic.Location == "Library.Authors.Registration.RegisterAuthor").Message.ShouldContain("AuthoringOnlyConstructs");
    [Fact] void should_report_provisioning_in_default_output() => Off.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse && diagnostic.Location == "Library.Authors.Registration.ProvisionAuthor").Message.ShouldContain("provisioning behavior");
    [Fact] void should_not_emit_unrecoverable_generation() => Result.Source.ShouldNotContain("generated");
    [Fact] void should_compile_and_reject_only_executable_admission() => AssertAuthoringDocument();
}
