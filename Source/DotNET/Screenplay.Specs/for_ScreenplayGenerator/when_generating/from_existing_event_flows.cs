// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_existing_event_flows : a_generated_document
{
    [Theory]
    [InlineData("log.Append(target, (object)@event);")]
    [InlineData("object e = @event; log.Append(target, e);")]
    [InlineData("object e = (object)@event; object forwarded = e; log.Append(target, forwarded);")]
    [InlineData("var payload = new { Event = @event }; System.Console.WriteLine(payload);")]
    [InlineData("var payload = new { Event = (object)@event }; System.Console.WriteLine(payload);")]
    [InlineData("log.AppendMany(target, new[] { @event });")]
    [InlineData("log.AppendMany(target, [@event]);")]
    [InlineData("log.AppendMany(target, new System.Collections.Generic.List<AuthorRegistered> { @event });")]
    [InlineData("var events = new[] { @event }; log.AppendMany(target, events);")]
    [InlineData("var events = new[] { @event }; log.AppendMany(target, [.. events]);")]
    [InlineData("Stored = @event;")]
    [InlineData("StoredEvents = new[] { @event };")]
    [InlineData("StoredEvents[0] = @event;")]
    [InlineData("var events = new[] { @event };")]
    [InlineData("AuthorRegistered[] events = [@event];")]
    [InlineData("var events = new System.Collections.Generic.List<AuthorRegistered> { @event };")]
    [InlineData("string name; (Stored, name) = (@event, @event.Name);")]
    public void should_keep_reappended_or_stored_events_standalone(string body)
    {
        GenerateWith("""
            public AuthorRegistered Stored { get; set; }
            public AuthorRegistered[] StoredEvents { get; set; }
            public void Handle(AuthorRegistered @event, EventContext context, Cratis.Chronicle.EventSequences.IEventLog log)
            {
                var target = Cratis.Chronicle.Events.EventSourceId.New();
            """ + body + "}");
        Result.Source.ShouldNotContain("produces event AuthorRegistered");
        Result.Model.EventProducerCounts.Values.Single().ShouldBeGreaterThan(1);
        AssertDocument();
    }

    [Theory]
    [InlineData("public void Forward(AuthorRegistered e, out object stored) { stored = e; }")]
    [InlineData("public void Forward(AuthorRegistered e, ref object stored) { stored = (object)e; }")]
    [InlineData("public System.Collections.Generic.IEnumerable<AuthorRegistered> Forward(AuthorRegistered e) => new[] { e };")]
    [InlineData("public System.Collections.Generic.IEnumerable<AuthorRegistered> Forward(AuthorRegistered e) { yield return e; }")]
    [InlineData("public System.Collections.Generic.IEnumerable<AuthorRegistered> Forward(System.Collections.Generic.IEnumerable<AuthorRegistered> events) => events;")]
    [InlineData("public (AuthorRegistered, string) Forward(AuthorRegistered e) => (e, e.Name);")]
    [InlineData("public void Forward((AuthorRegistered, string) value) { Accept(value); } static void Accept(object value) { }")]
    [InlineData("public void Forward(System.Collections.Generic.IEnumerable<AuthorRegistered> events) { Accept(events); } static void Accept(object value) { }")]
    [InlineData("public AuthorRegistered Stored; public void Forward(AuthorRegistered e) { Stored = e; }")]
    [InlineData("public AuthorRegistered[] Stored; public void Forward(AuthorRegistered[] events) { Stored = events; }")]
    [InlineData("public void Forward(AuthorRegistered e) { var wrapper = new Wrapper(e); } public record Wrapper(AuthorRegistered Event);")]
    [InlineData("public System.Func<AuthorRegistered> Forward(AuthorRegistered e) => () => e;")]
    public void should_count_helpers_returning_or_forwarding_event_containers(string helper)
    {
        GenerateWith(helper);
        Result.Source.ShouldNotContain("produces event AuthorRegistered");
        AssertDocument();
    }

    [Theory]
    [InlineData("var name = @event.Name; System.Console.WriteLine(name);")]
    [InlineData("if (@event is { Name: var name }) System.Console.WriteLine(name);")]
    [InlineData("System.Console.WriteLine(nameof(AuthorRegistered));")]
    [InlineData("AuthorRegistered local = @event;")]
    [InlineData("object local = @event;")]
    [InlineData("object local = (object)@event;")]
    public void should_allow_inlining_when_the_reactor_only_consumes_fields_or_keeps_a_local(string body)
    {
        GenerateWith("public void Handle(AuthorRegistered @event, EventContext context) { " + body + " }");
        Result.Source.ShouldContain("produces event AuthorRegistered");
        Result.Model.EventProducerCounts.Values.Single().ShouldEqual(1);
        AssertDocument();
    }

    [Theory]
    [InlineData("public System.Func<AuthorRegistered, string> ReadName() => e => e.Name;")]
    [InlineData("public System.Threading.Tasks.Task<System.Func<AuthorRegistered, string>> ReadName() => System.Threading.Tasks.Task.FromResult<System.Func<AuthorRegistered, string>>(e => e.Name);")]
    public void should_not_count_a_returned_field_mapping_delegate_as_an_event(string helper)
    {
        GenerateWith(helper);
        Result.Source.ShouldContain("produces event AuthorRegistered");
        Result.Model.EventProducerCounts.Values.Single().ShouldEqual(1);
        AssertDocument();
    }

    void GenerateWith(string members)
    {
        var command = IdentifierSources.With("""
            [Command] public record RegisterAuthor(AuthorId Id, string Name)
            {
                public AuthorRegistered Handle() => new(Name);
            }
            """);
        var reactor = """
            using Library.Authors.Registration;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.Reactors;
            namespace Library.Authors.Notifications;
            public class Notifications : IReactor
            {
            """ + members + "}";
        Generate((Analyzed.SlicePath, command), ("Library/Authors/Notifications/Notify.cs", reactor));
    }
}
