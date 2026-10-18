// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Http;
using Cratis.Arc.Validation;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver.given;

public class a_keyed_protected_read : protected_plain_models
{
    protected void SelectKey(object? key)
    {
        _context = _context with { Type = typeof(Lookup), Command = new Lookup(key) };
        _inner.ClearReceivedCalls();
        _stored.ClearReceivedCalls();
    }

    protected object? Read(bool nullable) => ReadParameter(Parameter(nullable));

    protected object? ReadParameter(ParameterInfo parameter)
    {
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(_context.Type);
        CommandTransaction.Current = _unit;
        using var frame = _unit.EnforceReadProtection();
        CommandDecisionReads.Begin(typeof(Lookup));
        try
        {
            return Resolve(parameter);
        }
        finally
        {
            CommandDecisionReads.End();
        }
    }

    protected static ParameterInfo Parameter(bool nullable) => typeof(Lookup).GetMethod(nullable ? nameof(Lookup.Optional) : nameof(Lookup.Handle))!.GetParameters()[0];

    protected static void AssertMissing(Exception failure, string key)
    {
        failure.ShouldBeOfExactType<ProtectedReadModelDoesNotExist>();
        var result = CommandResult.FromException(Cratis.Execution.CorrelationId.New(), failure);
        result.HasExceptions.ShouldBeFalse();
        result.IsValid.ShouldBeFalse();
        result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.Rule);
        result.ValidationResults.Single().Severity.ShouldEqual(ValidationResultSeverity.Error);
        result.ValidationResults.Single().Message.ShouldEqual($"Balance '{key}' does not exist.");
        EndpointRouteHelper.GetStatusCode(result.IsSuccess, result.IsAuthorized, result.IsValid).ShouldEqual(System.Net.HttpStatusCode.BadRequest);
    }

    protected void StubRead(string key, Balance? instance)
    {
        _inner.GetFor<Balance>(_unit, (ReadModelKey)key, Arg.Any<CancellationToken>())
            .Returns(for_CommandDecisionReads.DecisionFixtures.Protected(key, instance));
        if (instance is not null) _stored.Release(instance).Returns(instance);
        _inner.ClearReceivedCalls();
        _stored.ClearReceivedCalls();
    }

    [ReadProtection(ReadProtectionMode.On)]
    public record Lookup(object? Key)
    {
        public void Handle([LookUpBy(nameof(Key))] Balance balance) { }
        public void Optional([LookUpBy(nameof(Key))] Balance? balance) { }
    }

    [ReadProtection(ReadProtectionMode.On)]
    public record SourceLookup([property: Cratis.Chronicle.Keys.Key] CompositeKey Source)
    {
        public void Handle(Balance balance) { }
    }

    public record CompositeKey(AccountId Account, string Region);
}
