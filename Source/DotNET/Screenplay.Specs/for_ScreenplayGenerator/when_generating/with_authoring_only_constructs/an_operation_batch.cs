// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_authoring_only_constructs;

public class an_operation_batch : an_authoring_document
{
    void Because() => Generate("""
        using System.Threading;
        using System.Threading.Tasks;
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Microsoft.Extensions.Logging;
        namespace Library.Authors.Registration;
        public interface IMailer { Task Send(string name); }
        /// <summary>Sends the welcome message.</summary>
        public record SendWelcomeEmail(string Recipient) : ICommandOperation
        {
            public Task Execute(IMailer mailer, ILogger<SendWelcomeEmail> logger, CancellationToken cancellationToken) => mailer.Send(Recipient);
            public Task Compensate(CommandOperationFailure failure, IMailer mailer, CancellationToken cancellationToken) => Task.CompletedTask;
        }
        [EventType] public record AuthorRegistered(string Name);
        [Command] public record RegisterAuthor(string Name)
        {
            public (AuthorRegistered, CommandOperations) Handle() => (new(Name), [new SendWelcomeEmail(Name)]);
        }
        """);

    [Fact] void should_declare_the_system_once() => Result.Source.Split("system Mailer", StringSplitOptions.None).Length.ShouldEqual(2);
    [Fact] void should_emit_the_returned_operation() => Result.Source.ShouldContain("produces operation SendWelcomeEmail");
    [Fact] void should_map_the_input() => Result.Source.ShouldContain("recipient String = name");
    [Fact] void should_name_the_external_system() => Result.Source.ShouldContain("uses Mailer");
    [Fact] void should_preserve_the_summary() => Result.Source.ShouldContain("description \"Sends the welcome message.\"");
    [Fact] void should_attach_execute_and_compensation_separately() => Result.Source.Split("file Feature/Slice/Slice.cs", StringSplitOptions.None).Length.ShouldEqual(3);
    [Fact] void should_emit_the_compensation_phase() => Result.Source.ShouldContain("compensate");
    [Fact] void should_compile_and_reject_only_executable_admission() => AssertAuthoringDocument();
    [Fact] void should_omit_operations_when_disabled() => Off.Source.Contains("produces operation", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_report_the_opt_in_when_disabled() => Off.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandOperation).Message.ShouldContain("AuthoringOnlyConstructs");
}
