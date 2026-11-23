// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using System.Reflection;
using Cratis.Arc.Commands;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_CommandUnionMetadataGenerator;

public class when_generating_declarations : Specification
{
    string _source;
    string _repeatedSource;
    Diagnostic[] _diagnostics;
    Assembly _assembly;

    void Because()
    {
        var compilation = given.union_compilation.Create(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Cratis.Arc.Commands.ModelBound;
            namespace UnionDeclarations;
            #pragma warning disable CA1815
            public readonly union Answer(int, string)
            {
                public Answer(char value, byte flag) : this(42) { }
                internal Answer(float value) : this(42) { }
            }
            public readonly union Single(Answer);
            public readonly union Generic<T>(T, bool);
            public readonly union Nested(Answer, (Guid, Single));
            public readonly union PrivateReturn(short, byte);
            public readonly union WrappedReturn(double, char);
            public readonly union CollectionReturn(uint, ulong);
            #pragma warning restore CA1815
            public class Base { public Task<Nested?> Handle() => Task.FromResult<Nested?>(null); }
            [Command] public class Inherited : Base;
            [Command] public class Direct { public Answer Handle() => 42; }
            [Command] public class Again { public Answer Handle() => "again"; }
            [Command] public class Awaited { public ValueTask<Generic<decimal>> Handle() => new(new Generic<decimal>(1m)); }
            [Command] public class Collection { public List<CollectionReturn[]> Handle() => []; }
            [Command] public class Wrapped { public Cratis.Monads.Result<OneOf.OneOf<WrappedReturn, int>, string> Handle() => default; }
            [Command] public class Tuple { public (Single, Answer) Handle() => default; }
            public class Container<T>
            {
                [Command] private class Private { public PrivateReturn Handle() => (short)42; }
                [Command] public class Open { public Generic<T> Handle() => default; }
            }
            """,
            "UnionDeclarations");
        var result = given.union_compilation.Generate(compilation);
        _source = result.Source;
        _repeatedSource = given.union_compilation.Generate(compilation).Source;
        _diagnostics = result.Diagnostics;
        _assembly = given.union_compilation.LoadAndRegister(given.union_compilation.Emit(result.Output));
    }

    Type Answer => _assembly.GetType("UnionDeclarations.Answer")!;
    CommandUnionMetadata.Declaration Declaration => CommandUnionMetadata.For(Answer)!;

    [Fact] void should_compile_without_diagnostics() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_register_exact_cases_without_user_constructors() => Declaration.CaseTypes.ShouldEqual<Type>([typeof(int), typeof(string)]);
    [Fact] void should_run_the_generated_accessor() => Declaration.ActiveValue(Activator.CreateInstance(Answer, 42)!).ShouldEqual(42);
    [Fact] void should_use_a_delegate_from_the_generated_module() => Declaration.ActiveValue.Method.DeclaringType!.Assembly.ShouldEqual(_assembly);
    [Fact] void should_preserve_empty_active_values() => Declaration.ActiveValue(Activator.CreateInstance(Answer)!).ShouldBeNull();
    [Fact] void should_register_the_inherited_nested_return() => CommandUnionMetadata.For(_assembly.GetType("UnionDeclarations.Nested")!).ShouldNotBeNull();
    [Fact] void should_register_a_single_case_union() => CommandUnionMetadata.For(_assembly.GetType("UnionDeclarations.Single")!)!.CaseTypes.ShouldEqual<Type>([Answer]);
    [Fact] void should_register_a_closed_generic_union() => CommandUnionMetadata.For(_assembly.GetType("UnionDeclarations.Generic`1")!.MakeGenericType(typeof(decimal)))!.CaseTypes.ShouldEqual<Type>([typeof(decimal), typeof(bool)]);
    [Fact] void should_deduplicate_registrations() => _source.Split("Register(typeof(global::UnionDeclarations.Answer),").Length.ShouldEqual(2);
    [Fact] void should_not_emit_open_type_parameters() => _source.ShouldNotContain("Generic<T>");
    [Fact] void should_be_deterministic() => _source.ShouldEqual(_repeatedSource);
    [Fact] void should_register_private_generic_command_returns() => CommandUnionMetadata.For(_assembly.GetType("UnionDeclarations.PrivateReturn")!).ShouldNotBeNull();
    [Fact] void should_traverse_result_and_oneof() => CommandUnionMetadata.For(_assembly.GetType("UnionDeclarations.WrappedReturn")!).ShouldNotBeNull();
    [Fact] void should_traverse_collection_and_array_elements() => CommandUnionMetadata.For(_assembly.GetType("UnionDeclarations.CollectionReturn")!).ShouldNotBeNull();
}
#endif
