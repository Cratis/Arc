// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

[PerFactSpecificationLifecycle]
public class when_a_nullable_protected_key_is_absent : given.a_keyed_protected_read
{
    public static TheoryData<string?, bool, bool, string> AbsentKeys
    {
        get
        {
            var cases = new TheoryData<string?, bool, bool, string>();
            var combinations = from text in new string?[] { null, "", "   ", "\t" }
                               from concept in new[] { false, true }
                               from asynchronous in new[] { false, true }
                               from target in new[] { "Handle", "Provide", "Convention", "Validator" }
                               select (text, concept, asynchronous, target);
            foreach (var (text, concept, asynchronous, target) in combinations)
            {
                cases.Add(text, concept, asynchronous, target);
            }

            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(AbsentKeys))]
    public async Task should_resolve_null_without_acquiring_state(string? text, bool concept, bool asynchronous, string target)
    {
        object? key = concept && text is not null ? new AccountId(text) : text;
        SelectKey(key);
        var value = await ReadOptional(SelectParameter(target, key), asynchronous);

        value.ShouldBeNull();
        _inner.ReceivedCalls().ShouldBeEmpty();
        _stored.ReceivedCalls().ShouldBeEmpty();
        _unit.HasEnrolledDecisionReads.ShouldBeFalse();
    }

    [Fact]
    public void should_keep_the_existing_guid_empty_lookup_and_absence_guard()
    {
        SelectKey(Guid.Empty);
        StubRead(Guid.Empty.ToString(), null);

        Read(true).ShouldBeNull();

        _unit.HasEnrolledDecisionReads.ShouldBeTrue();
        _inner.Received(1).GetFor<Balance>(_unit, (ReadModelKey)Guid.Empty.ToString(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(DecisionReadRefusalReason.InvalidKey, typeof(ProtectedReadModelDoesNotExist))]
    [InlineData(DecisionReadRefusalReason.FoldIncomplete, typeof(ReadIncomplete))]
    [InlineData(DecisionReadRefusalReason.Join, typeof(ReadCannotBeProtected))]
    public async Task should_not_turn_a_protection_refusal_into_optional_absence(DecisionReadRefusalReason reason, Type failureType)
    {
        SelectKey("from");
        _inner.GetFor<Balance>(_unit, (ReadModelKey)"from", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DecisionRead<Balance>>(new DecisionReadRefused(reason, typeof(Balance))));

        var failure = await Catch.Exception(async () => await ReadOptional(Parameter(true), true));

        failure.GetType().ShouldEqual(failureType);
        _stored.ReceivedCalls().ShouldBeEmpty();
    }

    async Task<object?> ReadOptional(ParameterInfo parameter, bool asynchronous)
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

    ParameterInfo SelectParameter(string target, object? key)
    {
        if (target == "Convention")
        {
            var command = new Convention(key as AccountId ?? (key is string text ? new AccountId(text) : null));
            _context = _context with { Type = typeof(Convention), Command = command };
            return typeof(Convention).GetMethod(nameof(Convention.Handle))!.GetParameters()[0];
        }

        return target == "Validator"
            ? typeof(OptionalValidator).GetConstructors()[0].GetParameters()[0]
            : typeof(Targets).GetMethod(target)!.GetParameters()[0];
    }

    public record Targets
    {
        public void Handle([LookUpBy(nameof(Lookup.Key))] Balance? balance) { }
        public Balance? Provide([LookUpBy(nameof(Lookup.Key))] Balance? balance) => balance;
    }

    [ReadProtection(ReadProtectionMode.On)]
    public record Convention(AccountId? Account)
    {
        public void Handle(Balance? balance) { }
    }

    public class OptionalValidator([LookUpBy(nameof(Lookup.Key))] Balance? balance) : CommandValidator<Lookup>
    {
        public Balance? Balance { get; } = balance;
    }
}
