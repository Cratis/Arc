// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_rejecting_invalid_composite_parts : given.a_keyed_protected_read
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("*")]
    [InlineData("a#b")]
    [InlineData(" padded")]
    [InlineData("padded ")]
    public void should_check_each_part_before_converting_a_record_key(string part)
    {
        SelectKey(new CompositeKey(new("owner"), part));

        var failure = Catch.Exception(() => Read(false));

        AssertMissing(failure, string.Empty);
        _inner.ReceivedCalls().ShouldBeEmpty();
        _stored.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public void should_check_the_raw_composite_event_source_before_using_the_fallback_key()
    {
        var key = new CompositeKey(new("owner"), "*");
        _context = _context with { Type = typeof(SourceLookup), Command = new SourceLookup(key) };
        _context.Values[WellKnownCommandContextKeys.EventSourceId] = EventSourceExtensions.ToEventSourceId(key);
        _inner.ClearReceivedCalls();
        _stored.ClearReceivedCalls();

        var failure = Catch.Exception(() => ReadParameter(typeof(SourceLookup).GetMethod(nameof(SourceLookup.Handle))!.GetParameters()[0]));

        AssertMissing(failure, string.Empty);
        _inner.ReceivedCalls().ShouldBeEmpty();
        _stored.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_keep_a_resolved_target_override_instead_of_the_invalid_command_key(bool responseOverride)
    {
        var command = new SourceLookup(new(new("owner"), "*"));
        _context = _context with { Type = typeof(SourceLookup), Command = command };
        if (responseOverride) _context = _context with { Response = (Cratis.Chronicle.Events.EventSourceId)"from" };

        var instance = ReadParameter(typeof(SourceLookup).GetMethod(nameof(SourceLookup.Handle))!.GetParameters()[0]);

        instance.ShouldEqual(_from);
    }

    [Fact]
    public void should_unwrap_a_concept_part_inside_a_tuple_key()
    {
        SelectKey((new AccountId("*"), "region"));

        var failure = Catch.Exception(() => Read(true));

        AssertMissing(failure, string.Empty);
        _inner.ReceivedCalls().ShouldBeEmpty();
        _stored.ReceivedCalls().ShouldBeEmpty();
    }
}
