// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeScriptContentCombiner.when_combining;

public class with_a_field_call_mentioned_only_in_jsdoc : Specification
{
#pragma warning disable MA0136 // Raw String contains an implicit end of line character
    const string ParentContent = """
        /*---------------------------------------------------------------------------------------------
         *  **DO NOT EDIT** - This file is an automatically generated file.
         *--------------------------------------------------------------------------------------------*/

        /* eslint-disable sort-imports */
        import { field } from '@cratis/fundamentals';
        import { ChildType } from './ChildType';

        export class ParentType {
            @field(ChildType)
            child!: ChildType;
        }
        """;

    const string ChildContent = """
        /*---------------------------------------------------------------------------------------------
         *  **DO NOT EDIT** - This file is an automatically generated file.
         *--------------------------------------------------------------------------------------------*/

        /* eslint-disable sort-imports */
        import { field } from '@cratis/fundamentals';

        /**
         * Mentioning @field(ParentType) here documents an example; it is not a decorator invocation.
         */
        export class ChildType {
            @field(String)
            name!: string;
        }
        """;
#pragma warning restore MA0136 // Raw String contains an implicit end of line character

    string _result = null!;

    void Because() => _result = TypeScriptContentCombiner.Combine([ParentContent, ChildContent]);

    [Fact] void should_declare_the_child_before_the_parent() =>
        _result.IndexOf("export class ChildType", StringComparison.Ordinal)
            .ShouldBeLessThan(_result.IndexOf("export class ParentType", StringComparison.Ordinal));
    [Fact] void should_keep_the_documentation() => _result.ShouldContain("Mentioning @field(ParentType) here documents an example");
}
