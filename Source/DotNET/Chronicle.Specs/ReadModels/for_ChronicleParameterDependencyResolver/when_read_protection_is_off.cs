// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Arc.Validation;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_read_protection_is_off : given.a_keyed_protected_read
{
    [Theory]
    [InlineData("   ")]
    [InlineData("*")]
    [InlineData("a#b")]
    [InlineData(" padded ")]
    public async Task should_keep_reading_existing_legacy_keys(string key)
    {
        SelectKey(new AccountId(key));
        var model = new Balance(new(key));
        _stored.GetInstanceById(typeof(Balance), (ReadModelKey)key, default).Returns(model);
        _stored.Release(model).Returns(model);
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(OffCommand));

        var result = await ResolveArguments(false);

        result.Arguments.Single().ShouldBeSame(model);
        _inner.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task should_preserve_the_missing_identifier_message_and_reason()
    {
        SelectKey(new AccountId(string.Empty));
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(OffCommand));

        var failure = await Catch.Exception(async () => await ResolveArguments(false));

        failure.ShouldBeOfExactType<UnableToResolveReadModelFromCommandContext>();
        var result = ((IValidationFailure)failure).ValidationResult;
        result.Reason.ShouldEqual(ValidationResultReason.DependencyUnavailable);
        result.Message.ShouldEqual("The command is missing the identifier required to load its current state.");
        _stored.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task should_preserve_the_missing_required_model_message_and_reason()
    {
        SelectKey(new AccountId("missing"));
        _context.Values[CommandContextKeys.ResolvedKey] = "missing";
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(OffCommand));

        var failure = await Catch.Exception(async () => await ResolveArguments(false));

        failure.ShouldBeOfExactType<ReadModelDoesNotExistForCommand>();
        var result = ((IValidationFailure)failure).ValidationResult;
        result.Reason.ShouldEqual(ValidationResultReason.DependencyUnavailable);
        result.Message.ShouldEqual("The command targets an entity that does not exist.");
    }

    [Fact]
    public async Task should_preserve_nullable_absence()
    {
        SelectKey(new AccountId("missing"));
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(OffCommand));

        var result = await ResolveArguments(true);

        result.Arguments.Single().ShouldBeNull();
        _inner.ReceivedCalls().ShouldBeEmpty();
    }

    async Task<CommandHandlerArgumentResolution> ResolveArguments(bool nullable)
    {
        var handler = Substitute.For<ICommandHandler>();
        handler.Parameters.Returns([Parameter(nullable)]);
        return await new CommandHandlerArgumentResolver(new CommandProvideInvoker()).Resolve(handler, _context, _services, null);
    }

    [ReadProtection(ReadProtectionMode.Off)]
    public class OffCommand;
}
