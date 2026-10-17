// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.for_ModelGraphWalkerGenerator;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_DataAnnotationMemberTypes;

public class when_the_base_type_is_from_another_assembly : Specification
{
    string _source;
    Diagnostic[] _diagnostics;

    void Because()
    {
        const string librarySource = """
            using System.ComponentModel.DataAnnotations;
            namespace Library;
            public class Parent { [StringLength(3)] public string Label { get; set; } = "long"; }
            """;
        var library = ModelGraphWalkerCompilation.Library("AnnotatedBase", librarySource, true);
        const string models = """
            using System.ComponentModel.DataAnnotations;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Concepts;
            public record Text(string Value) : ConceptAs<string>(Value);
            [Command] public class Submit : Library.Parent
            {
                [Required] public Text Name { get; set; } = new("valid");
                public void Handle() { }
            }
            """;
        var result = ModelGraphWalkerCompilation.Compile("InheritedAnnotations", models, true, library);
        _source = result.Source;
        _diagnostics = result.Diagnostics;
    }

    [Fact] void should_compile_without_generated_warnings() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_register_the_derived_type() => _source.ShouldContain("DataAnnotationMembers.Register(typeof(global::Submit)");
    [Fact] void should_read_the_inherited_property() => _source.ShouldContain("((global::Submit)instance).@Label");
    [Fact] void should_reflect_the_inherited_attributes() => _source.ShouldContain("typeof(global::Library.Parent).GetProperty(\"Label\", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.DeclaredOnly)");
}
