// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeScriptContentCombiner.when_combining;

public class with_multiple_contents_having_documentation : Specification
{
#pragma warning disable MA0136 // Raw String contains an implicit end of line character
    const string FirstContent = """
        /*---------------------------------------------------------------------------------------------
         *  **DO NOT EDIT** - This file is an automatically generated file.
         *--------------------------------------------------------------------------------------------*/

        /* eslint-disable sort-imports */
        import { field } from '@cratis/fundamentals';

        /**
         * The first type.
         */
        export class FirstType {
        }
        """;

    const string SecondContent = """
        /*---------------------------------------------------------------------------------------------
         *  **DO NOT EDIT** - This file is an automatically generated file.
         *--------------------------------------------------------------------------------------------*/

        /* eslint-disable sort-imports */
        import { field } from '@cratis/fundamentals';

        /**
         * The second type.
         */
        export class SecondType {
        }
        """;
#pragma warning restore MA0136 // Raw String contains an implicit end of line character

    string _result = null!;

    void Because() => _result = TypeScriptContentCombiner.Combine([FirstContent, SecondContent]);

    [Fact] void should_contain_header_once() => _result.Split("DO NOT EDIT").Length.ShouldEqual(2);
    [Fact] void should_contain_only_one_eslint_disable() => _result.Split("/* eslint-disable sort-imports */").Length.ShouldEqual(2);
    [Fact] void should_contain_only_one_import() => _result.Split("import { field } from '@cratis/fundamentals';").Length.ShouldEqual(2);
    [Fact] void should_contain_first_type_documentation() => _result.ShouldContain("The first type.");
    [Fact] void should_contain_second_type_documentation() => _result.ShouldContain("The second type.");
    [Fact] void should_have_first_documentation_before_first_export() => _result.IndexOf("The first type.").ShouldBeLessThan(_result.IndexOf("export class FirstType"));
    [Fact] void should_have_second_documentation_before_second_export() => _result.IndexOf("The second type.").ShouldBeLessThan(_result.IndexOf("export class SecondType"));
    [Fact] void should_not_have_second_documentation_outside_jsdoc_block() => _result.ShouldNotContain("*/\n * The second type.");
    [Fact] void should_not_have_first_documentation_outside_jsdoc_block() => _result.ShouldNotContain("*/\n * The first type.");
}
