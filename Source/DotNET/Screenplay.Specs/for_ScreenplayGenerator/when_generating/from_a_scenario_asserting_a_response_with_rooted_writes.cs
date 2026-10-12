// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_scenario_asserting_a_response_with_rooted_writes : a_generated_document
{
    const string Response = "((CommandResult<Registration>)_result).Response";

    [Theory]
    [InlineData("void Change() => " + Response + ".Value = 1;")]
    [InlineData("void Change() => " + Response + ".Value += 1;")]
    [InlineData("void Change() => ++" + Response + ".Value;")]
    [InlineData("void Change() => --" + Response + ".Value;")]
    [InlineData("void Change() => " + Response + ".Value++;")]
    [InlineData("void Change() => " + Response + ".Value--;")]
    [InlineData("void Change() => Mutate(ref " + Response + ".Value); void Mutate(ref int value) { }")]
    [InlineData("void Change() => Mutate(out " + Response + ".Value); void Mutate(out int value) => value = 1;")]
    [InlineData("void Change() { ref var value = ref " + Response + ".Value; }")]
    [InlineData("ref int Change() => ref " + Response + ".Value;")]
    [InlineData("void Change() => (" + Response + ".Value, _) = (1, 2);")]
    [InlineData("void Change() => " + Response + ".Items[0].Value = 1;")]
    [InlineData("void Change() => " + Response + ".Numbers[0] = 1;")]
    [InlineData("void Change() => (_result as CommandResult<Registration>)?.Response.Items[0].Value = 1;")]
    [InlineData("void Change() => (_result as CommandResult<Registration>)?.Response.Numbers[0] = 1;")]
    [InlineData("void Copy() { var response = " + Response + "; }")]
    [InlineData("Registration _alias = null!; void Copy() => _alias = " + Response + ".Items[0];")]
    [InlineData("void Copy() { foreach (var item in " + Response + ".Items) { } }")]
    [InlineData("void Copy() { if (" + Response + " is { Items: var items }) { } }")]
    [InlineData("Registration Escape() { return " + Response + "; }")]
    [InlineData("Registration Escape => " + Response + ";")]
    [InlineData("System.Func<CommandResult> Escape() => () => _result;")]
    [InlineData("System.Collections.Generic.IEnumerable<CommandResult> Escape() { yield return _result; }")]
    public void should_withhold_a_response_with_rooted_writes_or_aliases(string members)
    {
        GenerateScenario(members);
        Result.Source.ShouldNotContain("specification");
        Result.Source.ShouldContain("returns");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain("CommandResult.Response");
        AssertDocument();
    }

    [Theory]
    [InlineData("void Read() => Observe(in " + Response + ".Value); void Observe(in int value) { }")]
    [InlineData("void Read() { var value = " + Response + ".Value; }")]
    [InlineData("void Read() { foreach (var number in " + Response + ".Numbers) { } }")]
    [InlineData("void Read() { foreach (var label in " + Response + ".Labels) { } }")]
    [InlineData("void Read() { if (_result is CommandResult<Registration>) { } }")]
    [InlineData("void Read() { _other.Response!.Value++; }")]
    [InlineData("void Read() { var number = _numbers[" + Response + ".Value]; }")]
    public void should_keep_a_response_with_only_reads_or_unrelated_writes(string members)
    {
        GenerateScenario(members);
        Result.Source.ShouldContain("then returns");
        Result.Source.ShouldContain("count = 1");
        AssertDocument();
    }

    [Fact]
    public void should_not_exempt_a_member_write_containing_the_action()
    {
        GenerateScenario("", Response + ".Value = (_result = await _scenario.Execute(new Register(1))).GetHashCode()");
        Result.Source.ShouldNotContain("specification");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain("CommandResult.Response");
        AssertDocument();
    }

    void GenerateScenario(string members, string action = "_result = await _scenario.Execute(new Register(1))")
    {
        const string slice = """
            using System.Collections.Generic;
            using Cratis.Arc.Commands.ModelBound;
            namespace Library.Registrations;
            public record Registration(int Count)
            {
                public int Value;
                public List<Registration> Items = new();
                public List<int> Numbers = new();
                public List<string> Labels = new();
            }
            [Command] public record Register(int Count)
            {
                public Registration Handle() => new Registration(Count);
            }
            """;
        const string testing = """
            using System.Threading.Tasks;
            using Cratis.Arc.Commands;
            namespace Cratis.Arc.Testing.Commands;
            public class CommandScenario<TCommand>
            {
                public Task<CommandResult> Execute(TCommand command) => Task.FromResult(new CommandResult());
            }
            """;
        var scenario = $$"""
            using System.Threading.Tasks;
            using Cratis.Arc.Commands;
            using Cratis.Arc.Testing.Commands;
            using Cratis.Specifications;
            using Library.Registrations;
            using Xunit;
            namespace Library.Registrations.when_registering;
            public class and_a_count_is_supplied
            {
                readonly CommandScenario<Register> _scenario = new();
                readonly CommandResult<Registration> _other = new(new Registration(1));
                readonly int[] _numbers = [1];
                CommandResult _result = null!;
                {{members}}
                async Task Because() => {{action}};
                [Fact] void should_return_the_count() => {{Response}}.Count.ShouldEqual(1);
            }
            """;
        (string Path, string Text)[] sources =
        [
            (Analyzed.SlicePath, slice),
            ("Library/Testing/Commands.cs", testing),
            ("Library/Registrations/when_registering/and_a_count_is_supplied.cs", scenario)
        ];
        Analyzed.ErrorsIn(sources).ShouldBeEmpty();
        Generate(sources);
    }
}
