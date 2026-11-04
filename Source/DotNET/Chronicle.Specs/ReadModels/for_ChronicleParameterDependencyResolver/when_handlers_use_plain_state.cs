// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_handlers_use_plain_state : given.protected_plain_models
{
    [Theory]
    [InlineData(ReadProtectionMode.Off)]
    [InlineData(ReadProtectionMode.Measure)]
    [InlineData(ReadProtectionMode.On)]
    public async Task should_resolve_the_default_event_source_key_and_feed_the_handler(ReadProtectionMode mode)
    {
        object command = mode switch
        {
            ReadProtectionMode.Off => new Off(),
            ReadProtectionMode.Measure => new Measure(),
            _ => new On()
        };
        _context = _context with { Command = command, Type = command.GetType() };
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(_context.Type);
        CommandTransaction.Current = _unit;
        CommandDecisionReads.Begin(_context.Type);
        try
        {
            var parameter = typeof(On).GetMethod(nameof(On.Handle))!.GetParameters()[0];
            var resolved = new ChronicleParameterDependencyResolver([typeof(Balance)]).TryResolve(_services, parameter, out var instance);
            resolved.ShouldEqual(mode == ReadProtectionMode.On);
            var model = (Balance)(resolved ? instance : await ReadModelServiceCollectionExtensions.ResolveReadModelAsync(typeof(Balance), _context, _stored))!;
            new On().Handle(model).Account.ShouldEqual(new AccountId("from"));
            _unit.HasEnrolledDecisionReads.ShouldEqual(mode == ReadProtectionMode.On);
            if (mode == ReadProtectionMode.On)
            {
                var token = await _services.GetRequiredService<IDecisionReads>().Get<Balance>((ReadModelKey)"from", global::Xunit.TestContext.Current.CancellationToken);
                token.Instance.ShouldBeSame(model);
                token.Key.ShouldEqual((ReadModelKey)"from");
                var scope = (ConcurrencyScope)typeof(IDecisionRead).GetProperty("Scope", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(token)!;
                scope.SequenceNumber.ShouldEqual((EventSequenceNumber)1ul);
            }
            else
            {
                _stored.ReceivedCalls().Single(call => call.GetMethodInfo().Name == nameof(IReadModels.GetInstanceById)).GetArguments()[1].ShouldEqual((ReadModelKey)"from");
            }
        }
        finally
        {
            CommandDecisionReads.End();
        }
    }

    [ReadProtection(ReadProtectionMode.On)]
    public record On
    {
        public Observed Handle(Balance balance) => new(balance.Id);
    }

    [ReadProtection(ReadProtectionMode.Off)]
    public record Off : On;

    [ReadProtection(ReadProtectionMode.Measure)]
    public record Measure : On;

    public record Observed(AccountId Account);
}
