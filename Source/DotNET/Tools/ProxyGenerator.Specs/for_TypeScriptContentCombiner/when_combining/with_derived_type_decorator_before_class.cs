// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeScriptContentCombiner.when_combining;

public class with_derived_type_decorator_before_class : Specification
{
#pragma warning disable MA0136 // Raw String contains an implicit end of line character
    const string BaseContent = """
        /*---------------------------------------------------------------------------------------------
         *  **DO NOT EDIT** - This file is an automatically generated file.
         *--------------------------------------------------------------------------------------------*/

        /* eslint-disable sort-imports */
        import { field, derivedType } from '@cratis/fundamentals';

        /**
         * Base interface.
         */
        export class IBase {

            @field(String)
            errorMessage!: string;
        }
        """;

    const string DerivedContent = """
        /*---------------------------------------------------------------------------------------------
         *  **DO NOT EDIT** - This file is an automatically generated file.
         *--------------------------------------------------------------------------------------------*/

        /* eslint-disable sort-imports */
        import { field, derivedType } from '@cratis/fundamentals';

        /**
         * A concrete derived type.
         */
        @derivedType('emailAddress')
        export class EmailAddress {

            @field(String)
            errorMessage?: string;
        }
        """;
#pragma warning restore MA0136 // Raw String contains an implicit end of line character

    string _result = null!;

    void Because() => _result = TypeScriptContentCombiner.Combine([BaseContent, DerivedContent]);

    [Fact] void should_place_decorator_before_class_declaration() =>
        _result.IndexOf("@derivedType('emailAddress')").ShouldBeLessThan(_result.IndexOf("export class EmailAddress"));
    [Fact] void should_not_place_decorator_before_import_statements() =>
        _result.IndexOf("@derivedType('emailAddress')").ShouldBeGreaterThan(_result.IndexOf("import {"));
    [Fact] void should_keep_documentation_before_decorator() =>
        _result.IndexOf("A concrete derived type.").ShouldBeLessThan(_result.IndexOf("@derivedType('emailAddress')"));
}
