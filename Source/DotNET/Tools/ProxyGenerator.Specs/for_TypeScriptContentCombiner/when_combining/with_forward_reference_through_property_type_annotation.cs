// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeScriptContentCombiner.when_combining;

public class with_forward_reference_through_property_type_annotation : Specification
{
#pragma warning disable MA0136 // Raw String contains an implicit end of line character
    const string ParentTypeContent = """
        /*---------------------------------------------------------------------------------------------
         *  **DO NOT EDIT** - This file is an automatically generated file.
         *--------------------------------------------------------------------------------------------*/

        /* eslint-disable sort-imports */
        // eslint-disable-next-line header/header
        import { field } from '@cratis/fundamentals';

        export class ParentType {
            @field(String)
            all!: ChildType;
        }
        """;

    const string ChildTypeContent = """
        /*---------------------------------------------------------------------------------------------
         *  **DO NOT EDIT** - This file is an automatically generated file.
         *--------------------------------------------------------------------------------------------*/

        /* eslint-disable sort-imports */
        // eslint-disable-next-line header/header
        import { field } from '@cratis/fundamentals';

        export class ChildType {
            @field(String)
            name!: string;
        }
        """;
#pragma warning restore MA0136 // Raw String contains an implicit end of line character

    string _result = null!;

    void Because() => _result = TypeScriptContentCombiner.Combine([ParentTypeContent, ChildTypeContent]);

    [Fact] void should_declare_child_type_before_parent() =>
        _result.IndexOf("export class ChildType", StringComparison.Ordinal)
            .ShouldBeLessThan(_result.IndexOf("export class ParentType", StringComparison.Ordinal));

    [Fact] void should_contain_parent_type() =>
        _result.ShouldContain("export class ParentType");

    [Fact] void should_contain_child_type() =>
        _result.ShouldContain("export class ChildType");
}
