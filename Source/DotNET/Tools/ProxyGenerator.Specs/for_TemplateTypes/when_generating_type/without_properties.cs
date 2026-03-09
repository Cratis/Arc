// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_TemplateTypes.when_generating_type;

/// <summary>
/// Specification for verifying that types without properties do not include the field import.
/// </summary>
public class without_properties : Specification
{
    string _result;

    void Establish()
    {
        var descriptor = new TypeDescriptor(
            typeof(EmptyType),
            "EmptyType",
            [],
            Enumerable.Empty<ImportStatement>().OrderBy(_ => _.Module),
            []);

        _result = TemplateTypes.Type(descriptor);
    }

    [Fact] void should_not_contain_field_import() => _result.ShouldNotContain("import { field } from '@cratis/fundamentals';");
    [Fact] void should_contain_export_class() => _result.ShouldContain("export class EmptyType");
}

public class EmptyType;
