// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup.given;

public static class Declarations
{
    [EventType]
    public record Credited(decimal Amount);

    [FromEvent<Credited>]
    public record Balance(Guid Id);

    [Cratis.Chronicle.EventSources.EventSource("StartupAdmission")]
    [Cratis.Chronicle.EventSources.EventStream("Balances")]
    public class BalanceSource : IEventSource;

    [EventStream<BalanceSource>("Balances"), FromEvent<Credited>]
    public record StreamBalance(Guid Id);

    public class PlainCommand
    {
        public void Handle(Balance balance) { }
    }

    [ReadProtection(ReadProtectionMode.On)]
    public class OnCommand
    {
        public void Handle(Balance balance) { }
    }

    [ProtectedDecision]
    public class WrappedCommand
    {
        public void Handle(DecisionRead<Balance> balance) { }
    }

    [ProtectedDecision, ReadProtection(ReadProtectionMode.On)]
    public class WrappedOnCommand
    {
        public void Handle(DecisionRead<Balance> balance) { }
    }

    [ProtectedDecision]
    public class ProtectedCommand
    {
        public void Handle(Balance balance) { }
    }

    [ReadProtection(ReadProtectionMode.On)]
    public class StreamCommand
    {
        public void Handle(StreamBalance balance, DecisionRead<StreamBalance> wrapped, IEnumerable<DecisionRead<StreamBalance>> multiple) { }
    }

    [ReadProtection(ReadProtectionMode.On)]
    public class ValidatorCommand
    {
        public void Handle() { }
    }

    public class BalanceValidator(Balance balance) : CommandValidator<ValidatorCommand>
    {
        public Balance Balance { get; } = balance;
    }

    [ReadProtection(ReadProtectionMode.On)]
    public class AdvisoryCommand
    {
        public void Handle([Unprotected("Display only")] Balance balance) { }
    }

    [ReadProtection(ReadProtectionMode.On)]
    public class UnprotectedMethodCommand
    {
        [Unprotected("No decision reads")]
        public void Handle(Balance balance) { }
    }

    [ReadProtection(ReadProtectionMode.Off)]
    public class OffCommand
    {
        public void Handle(Balance balance) { }
    }

    [ReadProtection(ReadProtectionMode.Measure)]
    public class MeasureCommand
    {
        public void Handle(Balance balance) { }
    }

    [ReadProtection(ReadProtectionMode.On)]
    public class ProvideCommand
    {
        public void Provide(Balance balance) { }
        public void Handle() { }
    }
}
