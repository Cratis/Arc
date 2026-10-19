// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_CommandReadModelKeyGeneration;

public class when_a_validator_declares_read_model_parameters : Specification
{
    const string Models = """
        using Cratis.Arc.Commands;
        namespace Validated;
        public record Account(System.Guid Id);
        public record Change(System.Guid AccountId, string Name);
        public class ChangeValidator : CommandValidator<Change>
        {
            public ChangeValidator(Account account) { }
        }
        """;

    string _source;
    Diagnostic[] _diagnostics;

    void Because() => (_source, _diagnostics, _) = CommandKeyCompilation.Compile("ValidatorRoots", Models);

    [Fact] void should_emit_compilable_registrations() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_register_the_validated_command() => _source.ShouldContain(".RegisterCommand(typeof(global::Validated.Change)");
    [Fact] void should_declare_the_key_of_the_validator_dependency() => _source.ShouldContain(".DeclareModelKey(typeof(global::Validated.Account), typeof(global::System.Guid))");
}
