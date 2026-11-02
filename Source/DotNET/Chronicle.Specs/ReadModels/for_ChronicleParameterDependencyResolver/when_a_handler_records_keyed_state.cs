// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_a_handler_records_keyed_state : given.protected_plain_models
{
    [Theory]
    [InlineData("to", 2)]
    [InlineData("from", 1)]
    public void should_use_the_resolved_instances_and_cache_equal_keys(string target, int readCount)
    {
        var command = new given.RecordTransfer(new("from"), new(target));
        _context = _context with { Type = command.GetType(), Command = command };
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(_context.Type);
        CommandTransaction.Current = _unit;
        CommandDecisionReads.Begin(_context.Type);
        try
        {
            var parameters = typeof(given.RecordTransfer).GetMethod(nameof(given.RecordTransfer.Handle))!.GetParameters();
            var from = (Balance)Resolve(parameters[0])!;
            var to = (Balance)Resolve(parameters[1])!;
            var recorded = command.Handle(from, to);
            recorded.From.ShouldEqual(_from.Id);
            recorded.To.ShouldEqual(new AccountId(target));
            _tokens.Count.ShouldEqual(readCount);
            _tokens.Select(token => token.Key).ShouldEqual(readCount == 1 ? [(ReadModelKey)"from"] : [(ReadModelKey)"from", (ReadModelKey)"to"]);
            _tokens.All(token => token.IsProtected).ShouldBeTrue();
            if (readCount == 1)
            {
                to.ShouldBeSame(from);
            }

            _unit.HasEnrolledDecisionReads.ShouldBeTrue();
        }
        finally
        {
            CommandDecisionReads.End();
        }
    }
}
