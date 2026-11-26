// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_CommandReadModelKeyGeneration;

/// <summary>
/// Verifies relationship getters across handlers, providers, validators and overrides.
/// </summary>
public class when_generating_a_chained_lookup : Specification
{
    const string Models = """
        namespace Cratis.Arc.Chronicle.ReadModels
        {
            public static class ChainedReadModelKeys
            {
                public static void RegisterModel(System.Type model, params CommandReadModelKeyProperty[] properties) { }
            }
        }
        namespace Chains
        {
            public class BaseModel { public System.Guid RelatedId { get; } }
            public class Model : BaseModel { public System.Guid Id { get; } public string Unreferenced { get; } = ""; }
            public class Target { public System.Guid Id { get; } }
            public class BaseChange
            {
                public virtual void Handle([Cratis.Arc.Commands.LookUpBy(nameof(source), nameof(Model.RelatedId))] Target target, Model source) { }
            }
            [Cratis.Arc.Commands.ModelBound.Command]
            public class Change : BaseChange
            {
                public System.Guid Root { get; }
                public override void Handle(Target target, Model source) { }
            }
        }
        """;

    string _source;
    Diagnostic[] _diagnostics;
    void Because() => (_source, _diagnostics, _) = CommandKeyCompilation.Compile("ChainedLookup", Models);
    [Fact] void should_emit_compilable_registrations() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_register_a_model_not_a_fake_command() => _source.ShouldContain("ChainedReadModelKeys.RegisterModel(typeof(global::Chains.Model)");
    [Fact] void should_record_the_property_type_and_typed_getter() => _source.ShouldContain("\"RelatedId\", typeof(global::System.Guid), static instance => ((global::Chains.Model)instance).@RelatedId");
    [Fact] void should_not_register_unreferenced_properties() => _source.ShouldNotContain(".@Unreferenced");
}
