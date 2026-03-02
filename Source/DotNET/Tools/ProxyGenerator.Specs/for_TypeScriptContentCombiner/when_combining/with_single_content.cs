// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeScriptContentCombiner.when_combining;

public class with_single_content : Specification
{
#pragma warning disable MA0136 // Raw String contains an implicit end of line character
    const string Content = """
        /*---------------------------------------------------------------------------------------------
         *  **DO NOT EDIT** - This file is an automatically generated file.
         *--------------------------------------------------------------------------------------------*/

        /* eslint-disable sort-imports */
        import { field } from '@cratis/fundamentals';

        export class MyType {
        }

        """;
#pragma warning restore MA0136 // Raw String contains an implicit end of line character

    string _result = null!;

    void Because() => _result = TypeScriptContentCombiner.Combine([Content]);

    [Fact] void should_return_same_content() => _result.ShouldEqual(Content);
}
