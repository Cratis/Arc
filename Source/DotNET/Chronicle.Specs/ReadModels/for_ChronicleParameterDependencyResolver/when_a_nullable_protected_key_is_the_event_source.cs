// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

[PerFactSpecificationLifecycle]
public class when_a_nullable_protected_key_is_the_event_source : given.a_keyed_protected_read
{
    public static TheoryData<string?, Type, string, bool> InvalidTargets
    {
        get
        {
            var cases = new TheoryData<string?, Type, string, bool>();
            var combinations = from text in new string?[] { null, "", "   ", "\t" }
                               from command in new[] { typeof(KeyTarget), typeof(PropertyKeyTarget), typeof(FallbackTarget), typeof(EventSourceTarget) }
                               from method in new[] { "Handle", "Explicit" }
                               from asynchronous in new[] { false, true }
                               select (text, command, method, asynchronous);
            foreach (var (text, command, method, asynchronous) in combinations)
            {
                cases.Add(text, command, method, asynchronous);
            }

            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(InvalidTargets))]
    public async Task should_refuse_an_absent_event_source_before_acquiring_state(string? text, Type commandType, string method, bool asynchronous)
    {
        var keyType = commandType.GetProperty("Account")!.PropertyType;
        var key = text is null ? null : keyType == typeof(string) ? text : Activator.CreateInstance(keyType, [text]);
        var command = Activator.CreateInstance(commandType, [key])!;
        SelectCommand(command);
        var parameter = commandType.GetMethod(method)!.GetParameters()[0];

        var failure = await Catch.Exception(async () => await ResolveTarget(parameter, asynchronous));

        AssertMissing(failure, text ?? string.Empty);
        _inner.ReceivedCalls().ShouldBeEmpty();
        _stored.ReceivedCalls().ShouldBeEmpty();
        _unit.HasEnrolledDecisionReads.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public async Task should_still_allow_an_absent_non_target_key(string? text)
    {
        SelectCommand(new NonTarget("target", text is null ? null : new AccountId(text)));
        var parameter = typeof(NonTarget).GetMethod(nameof(NonTarget.Handle))!.GetParameters()[0];

        var value = await ResolveTarget(parameter, true);

        value.ShouldBeNull();
        _inner.ReceivedCalls().ShouldBeEmpty();
        _stored.ReceivedCalls().ShouldBeEmpty();
        _unit.HasEnrolledDecisionReads.ShouldBeFalse();
    }

    void SelectCommand(object command)
    {
        var values = new EventSourceValuesProvider(NullLogger<EventSourceValuesProvider>.Instance).Provide(command);
        _context = _context with { Type = command.GetType(), Command = command, Values = values };
        _inner.ClearReceivedCalls();
        _stored.ClearReceivedCalls();
    }

    async Task<object?> ResolveTarget(ParameterInfo parameter, bool asynchronous)
    {
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(_context.Type);
        CommandTransaction.Current = _unit;
        using var frame = _unit.EnforceReadProtection();
        CommandDecisionReads.Begin(_context.Type);
        try
        {
            return asynchronous
                ? await ParameterDependencyResolver.ResolveAsync(_services, parameter, _ => new InvalidOperationException())
                : Resolve(parameter);
        }
        finally
        {
            CommandDecisionReads.End();
        }
    }

    [ReadProtection(ReadProtectionMode.On)]
    public record KeyTarget([Cratis.Chronicle.Keys.Key] AccountId Account)
    {
        public void Handle(Balance? balance) { }
        public void Explicit([LookUpBy(nameof(Account))] Balance? balance) { }
    }

    [ReadProtection(ReadProtectionMode.On)]
    public record PropertyKeyTarget([property: Cratis.Chronicle.Keys.Key] AccountId Account)
    {
        public void Handle(Balance? balance) { }
        public void Explicit([LookUpBy(nameof(Account))] Balance? balance) { }
    }

    [ReadProtection(ReadProtectionMode.On)]
    public record FallbackTarget([property: Cratis.Chronicle.Keys.Key] string Account)
    {
        public void Handle(Balance? balance) { }
        public void Explicit([LookUpBy(nameof(Account))] Balance? balance) { }
    }

    [ReadProtection(ReadProtectionMode.On)]
    public record EventSourceTarget(EventSourceId Account)
    {
        public void Handle(Balance? balance) { }
        public void Explicit([LookUpBy(nameof(Account))] Balance? balance) { }
    }

    [ReadProtection(ReadProtectionMode.On)]
    public record NonTarget([property: Cratis.Chronicle.Keys.Key] string Target, AccountId? Account)
    {
        public void Handle(Balance? balance) { }
    }
}
