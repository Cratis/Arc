// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeScriptContentCombiner.when_combining;

public class with_derived_class_extending_base_class : Specification
{
#pragma warning disable MA0136 // Raw String contains an implicit end of line character
    const string DerivedTypeContent = """
        /*---------------------------------------------------------------------------------------------
         *  **DO NOT EDIT** - This file is an automatically generated file.
         *--------------------------------------------------------------------------------------------*/

        /* eslint-disable sort-imports */
        // eslint-disable-next-line header/header
        import { field } from '@cratis/fundamentals';
        import { BaseType } from './BaseType';

        export class DerivedType extends BaseType {
            @field(Number)
            derivedProperty!: number;
        }
        """;

    const string BaseTypeContent = """
        /*---------------------------------------------------------------------------------------------
         *  **DO NOT EDIT** - This file is an automatically generated file.
         *--------------------------------------------------------------------------------------------*/

        /* eslint-disable sort-imports */
        // eslint-disable-next-line header/header
        import { field } from '@cratis/fundamentals';

        export class BaseType {
            @field(String)
            sharedProperty!: string;
        }
        """;
#pragma warning restore MA0136 // Raw String contains an implicit end of line character

    string _result = null!;

    void Because() => _result = TypeScriptContentCombiner.Combine([DerivedTypeContent, BaseTypeContent]);

    [Fact] void should_declare_base_type_before_derived_type() =>
        _result.IndexOf("export class BaseType", StringComparison.Ordinal)
            .ShouldBeLessThan(_result.IndexOf("export class DerivedType", StringComparison.Ordinal));

    [Fact] void should_not_import_base_type_defined_in_same_file() => _result.ShouldNotContain("import { BaseType }");

    [Fact] void should_contain_derived_type_extending_base() => _result.ShouldContain("export class DerivedType extends BaseType");

    [Fact] void should_contain_base_type_export() => _result.ShouldContain("export class BaseType");

    [Fact] void should_keep_fundamentals_import() => _result.ShouldContain("import { field } from '@cratis/fundamentals'");
}
