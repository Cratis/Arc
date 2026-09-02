// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Templates.for_Interface;

public class when_rendering_a_type_with_a_base_type : Specification
{
    TypeDescriptor _descriptor = null!;
    string _result = null!;

    void Establish()
    {
        var imports = new List<ImportStatement> { new(typeof(string), "RowDefinition", "./RowDefinition") }.ToOrderedImports();
        _descriptor = new TypeDescriptor(
            typeof(object),
            "Grid",
            [
                new PropertyDescriptor(typeof(string), "Rows", "RowDefinition", "RowDefinition", string.Empty, true, false, false, "Gets the rows, top to bottom."),
                new PropertyDescriptor(typeof(string), "Title", "string", "String", string.Empty, false, true, true, null)
            ],
            imports,
            [],
            "Lays its children out in rows and columns.",
            BaseTypeName: "Panel");
    }

    void Because() => _result = TemplateTypes.Interface(_descriptor);

    [Fact] void should_declare_an_interface() => _result.ShouldContain("export interface Grid extends Panel {");
    [Fact] void should_not_declare_a_class() => _result.ShouldNotContain("export class");
    [Fact] void should_not_decorate_any_property() => _result.ShouldNotContain("@field");
    [Fact] void should_not_import_fundamentals() => _result.ShouldNotContain("@cratis/fundamentals");
    [Fact] void should_keep_the_imports_the_properties_need() => _result.ShouldContain("import { RowDefinition } from './RowDefinition';");
    [Fact] void should_carry_the_type_documentation() => _result.ShouldContain("Lays its children out in rows and columns.");
    [Fact] void should_carry_the_property_documentation() => _result.ShouldContain("Gets the rows, top to bottom.");
    [Fact] void should_render_an_enumerable_property_as_an_array() => _result.ShouldContain("rows: RowDefinition[];");
    [Fact] void should_render_a_nullable_property_as_optional() => _result.ShouldContain("title?: string;");
}
