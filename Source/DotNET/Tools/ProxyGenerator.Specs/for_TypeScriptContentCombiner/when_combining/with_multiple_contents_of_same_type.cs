// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeScriptContentCombiner.when_combining;

public class with_multiple_contents_of_same_type : Specification
{
#pragma warning disable MA0136 // Raw String contains an implicit end of line character
    const string FirstContent = """
        /*---------------------------------------------------------------------------------------------
         *  **DO NOT EDIT** - This file is an automatically generated file.
         *--------------------------------------------------------------------------------------------*/

        /* eslint-disable sort-imports */
        import { field } from '@cratis/fundamentals';

        export class FirstType {
        }
        """;

    const string SecondContent = """
        /*---------------------------------------------------------------------------------------------
         *  **DO NOT EDIT** - This file is an automatically generated file.
         *--------------------------------------------------------------------------------------------*/

        /* eslint-disable sort-imports */
        import { field } from '@cratis/fundamentals';

        export class SecondType {
        }
        """;
#pragma warning restore MA0136 // Raw String contains an implicit end of line character

    string _result = null!;

    void Because() => _result = TypeScriptContentCombiner.Combine([FirstContent, SecondContent]);

    [Fact] void should_contain_header() => _result.ShouldContain("DO NOT EDIT");
    [Fact] void should_contain_only_one_eslint_disable() => _result.Split("/* eslint-disable sort-imports */").Length.ShouldEqual(2);
    [Fact] void should_contain_only_one_import() => _result.Split("import { field } from '@cratis/fundamentals';").Length.ShouldEqual(2);
    [Fact] void should_contain_first_type() => _result.ShouldContain("export class FirstType");
    [Fact] void should_contain_second_type() => _result.ShouldContain("export class SecondType");
}
