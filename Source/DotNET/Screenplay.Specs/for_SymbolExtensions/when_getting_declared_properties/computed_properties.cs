// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_SymbolExtensions.when_getting_declared_properties;

public class computed_properties : Specification
{
    const string Source = """
        namespace Library;

        public record Values(string Positional)
        {
            public string Initialized { get; init; } = string.Empty;
            public string Automatic { get; } = string.Empty;
            public string Expression => Positional;
            public string Getter { get { return Positional; } }
            public string GetterExpression { get => Positional; }
            public string Mutable { get => Positional; set { } }
        }

        public interface IValues
        {
            string Automatic { get; }
        }

        public abstract record AbstractValues
        {
            public abstract string Automatic { get; }
        }
        """;

    Compilation _compilation;

    void Establish() => _compilation = Analyzed.Compile((Analyzed.SlicePath, Source));

    IEnumerable<string> PropertiesOf(string name) => _compilation.GetTypeByMetadataName($"Library.{name}")!.DeclaredProperties().Select(property => property.Name);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_compilation).ShouldBeEmpty();
    [Fact] void should_keep_only_stored_properties() => PropertiesOf("Values").ShouldContainOnly(["Positional", "Initialized", "Automatic", "Mutable"]);
    [Fact] void should_keep_interface_properties_without_a_body() => PropertiesOf("IValues").ShouldContainOnly(["Automatic"]);
    [Fact] void should_keep_abstract_properties_without_a_body() => PropertiesOf("AbstractValues").ShouldContainOnly(["Automatic"]);
}
