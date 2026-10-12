// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_command_scenario_with_an_unrepresentable_caller : a_generated_document
{
    [Theory]
    [InlineData("new ClaimsPrincipal(new ClaimsIdentity([new Claim(\"tenant\", \"a\")], \"\"))")]
    [InlineData("new ClaimsPrincipal(new ClaimsIdentity([new Claim(\"tenant\", Computed())], \"test\"))")]
    [InlineData("new ClaimsPrincipal(new ClaimsIdentity([], \"test\", ClaimTypes.Name, \"custom-role\"))")]
    [InlineData("new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, \"\")], \"test\"))")]
    [InlineData("new ClaimsPrincipal(new[] { new ClaimsIdentity([], \"test\"), new ClaimsIdentity([], \"test\") })")]
    [InlineData("new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>(new[] { new Claim(\"hidden\", \"claim\") }) { new Claim(\"visible\", \"claim\") }, \"test\"))")]
    public void should_withhold_an_authorized_scenario_but_keep_an_open_scenario_with_a_diagnostic(string principal)
    {
        AssertCaller(CallerScenarioSources.Setup(principal), "[Authorize]", "SP0039", false);
        AssertCaller(CallerScenarioSources.Setup(principal), "", "SP0075", true);
    }

    [Theory]
    [InlineData("", "no caller")]
    [InlineData("if (DateTime.UtcNow.Ticks > 0) { SETUP }", "conditional")]
    [InlineData("SETUP _scenario.Services.AddSingleton(accessor);", "registration")]
    [InlineData("SETUP accessor.Current.Returns(new ClaimsPrincipal());", "configuration")]
    [InlineData("SETUP accessor.Current.AddIdentity(new ClaimsIdentity());", "used")]
    [InlineData("SETUP _scenario.Services.AddSingleton<ICurrentPrincipalOverride>(Substitute.For<ICurrentPrincipalOverride>());", "authorization service")]
    [InlineData("SETUP", "same command scenario")]
    [InlineData("SETUP _scenario.Services.AddSingleton(typeof(ICurrentPrincipalAccessor), accessor);", "registration")]
    [InlineData("var accessor = Substitute.For<ICurrentPrincipalAccessor>(); accessor.Current.Returns(PRINCIPAL); _scenario.Services.AddSingleton<object>(accessor);", "one accessor instance")]
    [InlineData("var accessor = Substitute.For<ICurrentPrincipalAccessor>(new object()); accessor.Current.Returns(PRINCIPAL); _scenario.Services.AddSingleton(accessor);", "constructor arguments")]
    [InlineData("_scenario.Execute(new RegisterAuthor(\"author\", \"Prior\")); SETUP", "initialized")]
    public void should_name_why_the_caller_is_not_proven(string setup, string reason)
    {
        setup = setup.Replace("SETUP", CallerScenarioSources.Setup(CallerScenarioSources.RolePrincipal), StringComparison.Ordinal)
            .Replace("PRINCIPAL", CallerScenarioSources.RolePrincipal, StringComparison.Ordinal);
        if (reason == "same command scenario")
        {
            setup = setup.Replace("_scenario.Services", "_other.Services", StringComparison.Ordinal);
        }
        AssertCaller(setup, "[Authorize]", "SP0039", false);
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == "SP0039").Message.ShouldContain(reason);
    }

    [Fact]
    public void should_withhold_an_authorized_host_driven_scenario()
    {
        Generate(CallerScenarioSources.With("", action: "_client.ExecuteCommand(\"/register\", new RegisterAuthor(\"author\", \"Apollo\"))"));
        Result.Source.ShouldNotContain($"specification {CallerScenarioSources.ScenarioName}");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == "SP0039" && diagnostic.Message.Contains("caller", StringComparison.Ordinal)).ShouldBeTrue();
        AssertDocument();
    }

    void AssertCaller(string setup, string authorization, string code, bool retained)
    {
        Generate(CallerScenarioSources.With(setup, authorization));
        Result.Source.Contains($"specification {CallerScenarioSources.ScenarioName}", StringComparison.Ordinal).ShouldEqual(retained);
        Result.Source.ShouldNotContain("given caller");
        var diagnostic = Result.Diagnostics.Single(diagnostic => diagnostic.Code == code);
        diagnostic.Severity.ShouldEqual(retained ? ScreenplayDiagnosticSeverity.Information : ScreenplayDiagnosticSeverity.Warning);
        AssertDocument();
    }
}
