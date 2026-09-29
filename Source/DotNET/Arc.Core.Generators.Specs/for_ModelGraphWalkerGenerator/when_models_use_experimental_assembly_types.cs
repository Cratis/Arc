// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_ModelGraphWalkerGenerator;

/// <summary>
/// Every type in an experimental referenced assembly is experimental, and naming one in generated code is an error the
/// consumer cannot suppress from their own files.
/// </summary>
public class when_models_use_experimental_assembly_types : Specification
{
    const string Models = """
        using Cratis.Arc.Commands.ModelBound;
        #pragma warning disable CS8305
        namespace Models;
        public record Item(string Name);
        public record Holder<T>(T Value, int Count);
        [Command] public record DoIt(Holder<ExpLib.Thing> Experimental, Holder<Item> Plain) { public void Handle() { } }
        """;

    string _source;
    Diagnostic[] _diagnostics;

    void Because()
    {
        var experimental = ModelGraphWalkerCompilation.Library(
            "ExperimentalLibrary",
            "using System.Diagnostics.CodeAnalysis; [assembly: Experimental(\"EXP001\")] namespace ExpLib { public class Thing { } }",
            false);
        (_source, _diagnostics, _) = ModelGraphWalkerCompilation.Compile("ExperimentalReferenceSpec", Models, true, experimental);
    }

    [Fact] void should_emit_compilable_walkers_without_diagnostics() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_not_register_types_naming_experimental_assembly_types() => _source.ShouldNotContain("ExpLib");
    [Fact] void should_register_other_types() => _source.ShouldContain("Register(typeof(global::Models.Holder<global::Models.Item>),");
}
