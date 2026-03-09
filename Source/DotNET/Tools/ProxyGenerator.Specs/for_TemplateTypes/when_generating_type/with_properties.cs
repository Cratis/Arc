// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_TemplateTypes.when_generating_type;

/// <summary>
/// Specification for verifying that types with properties include the field import.
/// </summary>
public class with_properties : Specification
{
    string _result;

    void Establish()
    {
        var properties = new[]
        {
            new PropertyDescriptor(typeof(string), "Name", "string", "String", string.Empty, false, false, true, null)
        };

        var descriptor = new TypeDescriptor(
            typeof(TypeWithProperties),
            "TypeWithProperties",
            properties,
            Enumerable.Empty<ImportStatement>().OrderBy(_ => _.Module),
            []);

        _result = TemplateTypes.Type(descriptor);
    }

    [Fact] void should_contain_field_import() => _result.ShouldContain("import { field } from '@cratis/fundamentals';");
    [Fact] void should_contain_field_decorator() => _result.ShouldContain("@field(String)");
    [Fact] void should_contain_property() => _result.ShouldContain("name!: string;");
}

public class TypeWithProperties
{
    public string Name { get; set; } = string.Empty;
}
