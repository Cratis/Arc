// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

/// <summary>
/// A reaction states the values the event carries. When the event's own declaration changes what it is given, carries
/// a value nothing gives it, or a constant would be written into the document differently, the construction is code.
/// </summary>
public class from_a_reactor_constructing_values_indirectly : a_reacting_application
{
    [Theory]
    [InlineData("public record Greeted(string Name) { public string Name { get; init; } = Name.ToUpperInvariant(); }", "new Greeted(@event.Name)")]
    [InlineData("public record Greeted(string Name) { public string Greeting => \"Welcome\"; }", "new Greeted(@event.Name)")]
    [InlineData("public record Greeted(string Name) { public string Greeting { get; init; } = \"Welcome\"; }", "new Greeted(@event.Name)")]
    [InlineData("public record Greeted(string Name) { public string Greeting { get; init; } = \"Welcome\"; }", "new Greeted(@event.Name) { Greeting = \"Hello\" }")]
    [InlineData("public record Greeted { string _name = \"\"; public string Name { get => _name; init => _name = value.Trim(); } }", "new Greeted { Name = @event.Name }")]
    [InlineData("public record Named(string Name); [EventType] public record Greeted(string Name) : Named(Name.Trim());", "new Greeted(@event.Name)")]
    [InlineData("public record Greeted(string Name, string Greeting);", "new Greeted(@event.Name, \" Welcome \")")]
    [InlineData("public record Greeted(string Name, string Greeting);", "new Greeted(@event.Name, \"Say \\\"hi\\\"\")")]
    [InlineData("public record Greeted(string Name, string Greeting);", "new Greeted(@event.Name, \"line\\nbreak\")")]
    [InlineData("public record Greeted(string Name, string Greeting);", "new Greeted(@event.Name, \"back\\\\slash\")")]
    [InlineData("public record Greeted(string Name, string Greeting);", "new Greeted(@event.Name, \"\")")]
    [InlineData("public record Greeted(string Name, long Number);", "new Greeted(@event.Name, 9007199254740993L)")]
    [InlineData("public record Greeted(string Name, decimal Amount);", "new Greeted(@event.Name, 0.1234567890123456789m)")]
    public void should_keep_pointing_at_the_file(string @event, string construction)
    {
        GenerateWith($$"""
            [EventType] {{@event}}
            public class Greeter : IReactor { public Greeted Greet(AuthorRegistered @event) => {{construction}}; }
            """);

        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        Result.Source.ShouldNotContain("produces Greeted");
        AssertDocument();
    }

    [Theory]
    [InlineData("public record Greeted(string Name) { public string Greeting { get; init; } }", "new Greeted(@event.Name) { Greeting = \"Welcome\" }")]
    [InlineData("public record Greeted(string Name, string Greeting);", "new Greeted(@event.Name, \"Welcome back\")")]
    [InlineData("public record Greeted(string Name, decimal Amount);", "new Greeted(@event.Name, 1.5m)")]
    public void should_state_a_direct_construction(string @event, string construction)
    {
        GenerateWith($$"""
            [EventType] {{@event}}
            public class Greeter : IReactor { public Greeted Greet(AuthorRegistered @event) => {{construction}}; }
            """);

        Result.Source.ShouldContain("produces Greeted");
        Result.Source.ShouldNotContain("file Authors/Welcoming/Welcoming.cs");
        AssertDocument();
    }
}
