// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeScriptContentCombiner.when_combining;

public class with_multiple_contents_of_different_types : Specification
{
    const string _commandContent = """
        /*---------------------------------------------------------------------------------------------
         *  **DO NOT EDIT** - This file is an automatically generated file.
         *--------------------------------------------------------------------------------------------*/

        /* eslint-disable sort-imports */
        import { Command } from '@cratis/arc/commands';

        export class MyCommand {
        }
        """;

    const string _typeContent = """
        /*---------------------------------------------------------------------------------------------
         *  **DO NOT EDIT** - This file is an automatically generated file.
         *--------------------------------------------------------------------------------------------*/

        /* eslint-disable sort-imports */
        import { field } from '@cratis/fundamentals';

        export class MyType {
        }
        """;

    string _result = null!;

    void Because() => _result = TypeScriptContentCombiner.Combine([_commandContent, _typeContent]);

    [Fact] void should_contain_header_once() => _result.Split("DO NOT EDIT").Length.ShouldEqual(2);
    [Fact] void should_contain_command_import() => _result.ShouldContain("import { Command } from '@cratis/arc/commands';");
    [Fact] void should_contain_type_import() => _result.ShouldContain("import { field } from '@cratis/fundamentals';");
    [Fact] void should_contain_command_export() => _result.ShouldContain("export class MyCommand");
    [Fact] void should_contain_type_export() => _result.ShouldContain("export class MyType");
}
