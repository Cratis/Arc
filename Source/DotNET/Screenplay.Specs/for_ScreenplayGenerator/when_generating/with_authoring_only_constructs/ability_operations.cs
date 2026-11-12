// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_authoring_only_constructs;

public class ability_operations : an_authoring_document
{
    void Because() => Generate("""
        using System;
        using System.Threading;
        using System.Threading.Tasks;
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.Abilities;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;
        namespace Library.Orders.Placement;
        public record OrderId(Guid Value) : ConceptAs<Guid>(Value);
        public interface ILedger : ISystemAbilities
        {
            Task ReserveRevenue(OrderId orderId, CancellationToken cancellationToken);
            Task ReleaseRevenue(OrderId orderId, CancellationToken cancellationToken);
        }
        public interface IMailer : ISystemAbilities
        {
            Task NotifyAsync(OrderId orderId, CancellationToken cancellationToken);
        }
        [EventType] public record OrderPlaced(OrderId OrderId);
        public record ReserveOrderRevenue(OrderId OrderId) : ICommandOperation
        {
            public async Task Execute(ILedger ledger, IMailer mailer, CancellationToken cancellationToken)
            {
                await ledger.ReserveRevenue(OrderId, cancellationToken);
                await mailer.NotifyAsync(OrderId, cancellationToken);
            }

            public Task Compensate(ILedger ledger, CancellationToken cancellationToken) => ledger.ReleaseRevenue(OrderId, cancellationToken);
        }
        public record Sneaky(OrderId OrderId) : ICommandOperation
        {
            public async Task Execute(ILedger ledger, CancellationToken cancellationToken)
            {
                Console.WriteLine("hello");
                await ledger.ReserveRevenue(OrderId, cancellationToken);
            }
        }
        [Command] public record PlaceOrder(OrderId OrderId)
        {
            public (OrderPlaced, ReserveOrderRevenue) Handle() => (new(OrderId), new ReserveOrderRevenue(OrderId));
        }
        [Command] public record PlaceSneaky(OrderId OrderId)
        {
            public (OrderPlaced, Sneaky) Handle() => (new(OrderId), new Sneaky(OrderId));
        }
        """);

    [Fact] void should_declare_the_operation() => Result.Source.ShouldContain("produces operation ReserveOrderRevenue");
    [Fact] void should_use_the_first_system() => Result.Source.ShouldContain("uses Ledger");
    [Fact] void should_invoke_the_first_ability() => Result.Source.ShouldContain("invokes Ledger.ReserveRevenue");
    [Fact] void should_invoke_an_ability_of_another_system() => Result.Source.ShouldContain("invokes Mailer.Notify");
    [Fact] void should_invoke_the_compensating_ability() => Result.Source.ShouldContain("invokes Ledger.ReleaseRevenue");
    [Fact] void should_declare_each_system_that_is_invoked() => Result.Source.ShouldContain("system Mailer");
    [Fact] void should_declare_the_abilities_of_a_system() => Result.Source.ShouldContain("ability ReserveRevenue");
    [Fact] void should_type_an_ability_input_from_the_collaborator_method() => Result.Source.ShouldContain("ability Notify\n    orderId OrderId".ReplaceLineEndings());
    [Fact] void should_not_invent_an_effect_class() => Result.Source.Contains("idempotent", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_map_the_ability_input_to_the_operation_input() => Result.Source.ShouldContain("orderId = orderId");
    [Fact] void should_not_attach_the_source_file_to_an_invoking_phase() => Result.Source.Contains("file \"", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_leave_a_phase_that_does_more_than_call_abilities_in_code() => Result.Source.Contains("produces operation Sneaky", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_report_why_it_was_left_in_code() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandOperation && diagnostic.Message.Contains("does something other than call abilities", StringComparison.Ordinal)).ShouldBeTrue();
}
