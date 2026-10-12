// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_scenario_pinning_a_literal_value : a_generated_document
{
    void Because()
    {
        const string Command = """
            using System;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Concepts;
            namespace Library.Authors.Registration;
            public record ReceiptId(Guid Value) : ConceptAs<Guid>(Value), IGeneratable<ReceiptId>
            {
                public static ReceiptId New() => new(GenerateValue.Uuid());
            }
            [Command] public record AllocateReceipt
            {
                public ReceiptId Handle([GeneratedValue] ReceiptId receipt) => receipt;
            }
            """;
        const string Testing = """
            using System.Threading.Tasks;
            using Cratis.Arc.Commands;
            namespace Cratis.Arc.Testing.Commands;
            public class CommandScenario<TCommand>
            {
                public Task<CommandResult> Execute(TCommand command) => Task.FromResult(new CommandResult());
            }
            """;
        const string Scenario = """
            using System;
            using System.Threading.Tasks;
            using Cratis.Arc.Commands;
            using Cratis.Arc.Testing.Commands;
            using Xunit;
            namespace Library.Authors.Registration.when_allocating;
            public class and_a_receipt_is_pinned
            {
                readonly CommandScenario<AllocateReceipt> _scenario = new();
                CommandResult _result = null!;
                void Establish() => _scenario.PinGeneratedValue("receipt", new ReceiptId(Guid.Parse("AAAAAAAA-BBBB-4CCC-8DDD-EEEEEEEEEEEE")));
                async Task Because() => _result = await _scenario.Execute(new AllocateReceipt());
                [Fact] void should_return_the_receipt() => Assert.Equal(new ReceiptId(Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee")), ((CommandResult<ReceiptId>)_result).Response);
            }
            """;
        Generate(
            (Analyzed.SlicePath, Command),
            ("Library/Testing/Scenario.cs", Testing),
            ("Library/Testing/Pins.cs", GeneratedValueSources.Pins),
            ("Library/Authors/Registration/when_allocating/and_a_receipt_is_pinned.cs", Scenario));
    }

    [Fact] void should_state_a_generated_value_without_an_identifier() => Result.Source.ShouldContain("receipt ReceiptId generated\n");
    [Fact] void should_state_the_named_fixture() => Result.Source.ShouldContain("generated receipt = \"aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee\"");
    [Fact] void should_keep_the_expected_response() => Result.Source.ShouldContain("then returns \"aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee\"");
    [Fact] void should_not_emit_an_identifier_fixture() => Result.Source.ShouldNotContain("for \"");
    [Fact] void should_bind_and_round_trip() => AssertDocument();
    [Fact] void should_pass_reference_execution() => Run("WhenAllocatingAndAReceiptIsPinned").Passed.ShouldBeTrue();
}
