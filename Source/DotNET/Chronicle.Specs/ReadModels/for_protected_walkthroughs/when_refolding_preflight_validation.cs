// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.ReadModels.for_protected_walkthroughs;

public class when_refolding_preflight_validation : given.a_protected_read
{
    Borrow _command;
    EventSequenceNumber _before;
    EventSequenceNumber _afterPreflight;
    BookStock _first;
    BookStock _fresh;

    void Establish()
    {
        _command = new Borrow(_book, _member);
        _context = new(
            Cratis.Execution.CorrelationId.New(),
            typeof(Borrow),
            _command,
            [],
            new() { [WellKnownCommandContextKeys.EventSourceId] = _book },
            ServiceProvider: _services);
    }

    async Task Because()
    {
        await Seed();
        var validationBegin = typeof(CommandValidationExecution).GetMethod("Begin", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        _before = await _store.EventLog.GetTailSequenceNumber();
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Borrow));
        using (var validation = (IDisposable)validationBegin.Invoke(null, [typeof(Borrow)])!)
        {
            _first = Validate(_command).Stock;
        }
        _afterPreflight = await _store.EventLog.GetTailSequenceNumber();
        await Compete();
        Begin(_command);
        try
        {
            _fresh = Validate(_command).Stock;
        }
        finally
        {
            _scope.End(_context);
        }
    }

    [Fact]
    void should_refold_preflight_validation_without_writing_or_reusing_its_read_for_execution()
    {
        _first.Available.ShouldEqual(1);
        _afterPreflight.ShouldEqual(_before);
        _fresh.Available.ShouldEqual(0);
        ReferenceEquals(_fresh, _first).ShouldBeFalse();
    }
}
