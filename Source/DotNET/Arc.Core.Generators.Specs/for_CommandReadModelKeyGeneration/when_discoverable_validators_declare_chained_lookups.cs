// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Arc.Generators.Specs.for_ModelGraphWalkerGenerator;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_CommandReadModelKeyGeneration;

/// <summary>
/// Verifies generated relationship metadata for supported validator implementations.
/// </summary>
public class when_discoverable_validators_declare_chained_lookups : Specification
{
    const string Models = """
        namespace Cratis.Arc.Chronicle.ReadModels
        {
            public static class ChainedReadModelKeys
            {
                public static readonly System.Collections.Generic.Dictionary<System.Type, CommandReadModelKeyProperty[]> Models = new();
                public static void RegisterModel(System.Type model, params CommandReadModelKeyProperty[] properties) => Models[model] = properties;
            }
        }
        namespace ValidatorChains
        {
            public record Source(System.Guid Id, System.Guid Direct, System.Guid Inherited, System.Guid Existing);
            public record Target(System.Guid Id);
            public record DirectChange(System.Guid Root) { public void Handle() { } }
            public record InheritedChange(System.Guid Root) { public void Handle() { } }
            public record ExistingChange(System.Guid Root) { public void Handle() { } }
            public class DirectValidator([Cratis.Arc.Commands.LookUpBy(nameof(source), nameof(Source.Direct))] Target target, Source source)
                : FluentValidation.AbstractValidator<DirectChange>, Cratis.Arc.Validation.IDiscoverableValidator<DirectChange>
            {
                public Target Target { get; } = target;
                public Source Source { get; } = source;
            }
            public class ValidatorBase<T> : FluentValidation.AbstractValidator<T>, Cratis.Arc.Validation.IDiscoverableValidator<T> { }
            public class InheritedValidator([Cratis.Arc.Commands.LookUpBy(nameof(source), nameof(Source.Inherited))] Target target, Source source)
                : ValidatorBase<InheritedChange>
            {
                public Target Target { get; } = target;
                public Source Source { get; } = source;
            }
            public class ExistingValidator([Cratis.Arc.Commands.LookUpBy(nameof(source), nameof(Source.Existing))] Target target, Source source)
                : Cratis.Arc.Commands.CommandValidator<ExistingChange>
            {
                public Target Target { get; } = target;
                public Source Source { get; } = source;
            }
        }
        """;

    Diagnostic[] _diagnostics;
    object[] _properties;
    object _model;
    readonly Guid _direct = Guid.NewGuid();

    void Because()
    {
        var (_, diagnostics, output) = CommandKeyCompilation.Compile("DiscoverableValidatorChains", Models);
        _diagnostics = diagnostics;
        var assembly = ModelGraphWalkerCompilation.Load(output);
        var model = assembly.GetType("ValidatorChains.Source")!;
        var registry = (IDictionary)assembly.GetType("Cratis.Arc.Chronicle.ReadModels.ChainedReadModelKeys")!.GetField("Models")!.GetValue(null)!;
        _properties = ((Array)registry[model]!).Cast<object>().ToArray();
        _model = Activator.CreateInstance(model, Guid.NewGuid(), _direct, Guid.NewGuid(), Guid.NewGuid())!;
    }

    [Fact] void should_compile_without_diagnostics() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_register_a_direct_discoverable_validator_chain() => Names().ShouldContain("Direct");
    [Fact] void should_register_an_inherited_discoverable_validator_chain() => Names().ShouldContain("Inherited");
    [Fact] void should_preserve_command_validator_chains() => Names().ShouldContain("Existing");
    [Fact] void should_read_the_registered_relationship_key() => Read("Direct").ShouldEqual(_direct);

    IEnumerable<string> Names() => _properties.Select(property => (string)property.GetType().GetProperty("Name")!.GetValue(property)!);

    object? Read(string name)
    {
        var property = _properties.Single(property => (string)property.GetType().GetProperty("Name")!.GetValue(property)! == name);
        return ((Func<object, object?>)property.GetType().GetProperty("Read")!.GetValue(property)!)(_model);
    }
}
