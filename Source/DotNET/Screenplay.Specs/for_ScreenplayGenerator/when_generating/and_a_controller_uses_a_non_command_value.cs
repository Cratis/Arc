// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class and_a_controller_uses_a_non_command_value : a_generated_document
{
    const string Inputs = "[FromBody] Payload command, [FromServices] Payload service, [FromQuery] Payload query, [FromRoute] Payload route, Payload injected";

    [Theory]
    [InlineData("service.Text")]
    [InlineData("query.Text")]
    [InlineData("route.Text")]
    [InlineData("injected.Text")]
    [InlineData("local.Text")]
    public void should_not_treat_another_parameter_or_local_as_the_command(string expression) =>
        AssertWithheld(Inputs, $"var local = command; return new MessagePosted({expression});");

    [Theory]
    [InlineData("FromServices")]
    [InlineData("FromQuery")]
    [InlineData("FromRoute")]
    public void should_not_treat_a_single_non_body_model_as_the_command(string binding) =>
        AssertWithheld($"[{binding}] Payload service", "return new MessagePosted(service.Text);");

    [Fact]
    public void should_not_treat_a_reassigned_command_as_its_original_input() =>
        AssertWithheld(Inputs, "command = new Payload(new MessageText(\"changed\")); return new MessagePosted(command.Text);");

    static string Source(string parameters, string body) => $$"""
        using System;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;
        using Microsoft.AspNetCore.Mvc;

        namespace Microsoft.AspNetCore.Mvc
        {
            public abstract class ControllerBase;
            [AttributeUsage(AttributeTargets.Method)] public sealed class HttpPostAttribute : Attribute;
            [AttributeUsage(AttributeTargets.Parameter)] public sealed class FromBodyAttribute : Attribute;
            [AttributeUsage(AttributeTargets.Parameter)] public sealed class FromServicesAttribute : Attribute;
            [AttributeUsage(AttributeTargets.Parameter)] public sealed class FromQueryAttribute : Attribute;
            [AttributeUsage(AttributeTargets.Parameter)] public sealed class FromRouteAttribute : Attribute;
        }

        namespace Library.Messaging.Feed
        {
            public record MessageText(string Value) : ConceptAs<string>(Value);
            public record Payload(MessageText Text);
            [EventType] public record MessagePosted(MessageText Text);
            public class FeedController : ControllerBase
            {
                [HttpPost]
                public MessagePosted Post({{parameters}}) { {{body}} }
            }
        }
        """;

    void AssertWithheld(string parameters, string body)
    {
        foreach (var authoring in new[] { false, true })
        {
            Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, Source(parameters, body)));
            Result.Source.ShouldNotContain("produces MessagePosted");
            Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction &&
                diagnostic.Severity == ScreenplayDiagnosticSeverity.Information && diagnostic.Message.Contains("Text")).ShouldBeTrue();
            AssertPartialDocument();
        }
    }
}
