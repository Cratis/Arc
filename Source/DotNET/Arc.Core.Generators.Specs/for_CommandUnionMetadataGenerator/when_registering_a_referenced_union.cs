// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using System.Reflection;
using Cratis.Arc.Commands;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_CommandUnionMetadataGenerator;

public class when_registering_a_referenced_union : Specification
{
    Assembly _library;
    Assembly _consumer;
    Diagnostic[] _diagnostics;

    void Because()
    {
        var library = given.union_compilation.Generate(given.union_compilation.Create(
            """
            using Cratis.Arc.Commands.ModelBound;
            namespace UnionLibrary;
            public readonly union Imported(long, string)
            {
                public Imported(char value, byte flag) : this(1L) { }
                internal Imported(float value) : this(1L) { }
            }
            [Command] public class Local { public Imported Handle() => 1L; }
            """,
            $"UnionLibrary_{Guid.NewGuid():N}"));
        var binary = given.union_compilation.Emit(library.Output);

        // Only enqueue the module. Registry lookup must drain it through EnsureRegistered, not a direct module call.
        _library = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(binary));
        GeneratedMetadataRegistration.Default.Register(GetType().Assembly, [new(_library.GetName().Name!, () => _library.ManifestModule)], []);
        CommandUnionMetadata.For(_library.GetType("UnionLibrary.Imported")!).ShouldNotBeNull();
        var consumer = given.union_compilation.Generate(given.union_compilation.Create(
            """
            using Cratis.Arc.Commands.ModelBound;
            [Command] public class ImportedCommand { public UnionLibrary.Imported Handle() => 2L; }
            """,
            "UnionConsumer",
            MetadataReference.CreateFromImage(binary)));
        _diagnostics = consumer.Diagnostics;
        _consumer = given.union_compilation.LoadAndRegister(given.union_compilation.Emit(consumer.Output));
    }

    CommandUnionMetadata.Declaration Declaration => CommandUnionMetadata.For(_library.GetType("UnionLibrary.Imported")!)!;
    [Fact] void should_compile_imported_case_metadata_without_diagnostics() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_ignore_imported_user_constructors() => Declaration.CaseTypes.ShouldEqual<Type>([typeof(long), typeof(string)]);
    [Fact] void should_allow_equivalent_registration_from_the_consumer() => Declaration.ActiveValue.Method.DeclaringType!.Assembly.ShouldEqual(_consumer);
    [Fact] void should_activate_the_library_through_pending_registration() => _library.GetType("Cratis.Arc.Commands.Generated.CommandUnionRegistration").ShouldNotBeNull();
}
#endif
