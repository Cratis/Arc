// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_explicit_lookup_keys_use_custom_wire_text : given.a_keyed_protected_read
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("*")]
    [InlineData("a#b")]
    [InlineData(" padded ")]
    public void should_reject_invalid_wire_text_before_acquiring_state(string text)
    {
        SelectKey(new WireKey(text));

        var failure = Catch.Exception(() => Read(false));

        AssertMissing(failure, string.Empty);
        _inner.ReceivedCalls().ShouldBeEmpty();
        _stored.ReceivedCalls().ShouldBeEmpty();
        _unit.HasEnrolledDecisionReads.ShouldBeFalse();
    }

    [Fact]
    public void should_read_using_valid_custom_wire_text()
    {
        SelectKey(new WireKey("from"));

        var instance = Read(false);

        instance.ShouldBeSame(_from);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("*")]
    [InlineData("a#b")]
    [InlineData(" padded ")]
    [InlineData("from")]
    public void should_keep_legacy_wire_keys_without_protection(string text)
    {
        SelectKey(new WireKey(text));

        foreach (var mode in new[] { ReadProtectionMode.Off, ReadProtectionMode.Measure })
        {
            CommandReadModelLookup.Resolve(_context, Parameter(false), mode).Value.ShouldEqual(text);
        }
    }

    [Theory]
    [InlineData(ReadProtectionMode.Off)]
    [InlineData(ReadProtectionMode.Measure)]
    public void should_preserve_the_legacy_empty_identifier_failure(ReadProtectionMode mode)
    {
        SelectKey(new WireKey(string.Empty));

        var failure = Catch.Exception(() => CommandReadModelLookup.Resolve(_context, Parameter(false), mode));

        failure.ShouldBeOfExactType<UnableToResolveReadModelFromCommandContext>();
        var result = ((IValidationFailure)failure).ValidationResult;
        result.Reason.ShouldEqual(ValidationResultReason.DependencyUnavailable);
        result.Message.ShouldEqual("The command is missing the identifier required to load its current state.");
    }

    public class WireKey(string text)
    {
        public override string ToString() => text;
    }
}
