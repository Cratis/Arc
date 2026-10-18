// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeScriptContentCombiner.when_combining;

public class with_headerless_templates : Specification
{
    string _result = null!;

    void Because()
    {
        var first = $"{FileHeader.GeneratedNotice}\nimport {{ FirstDependency }} from './FirstDependency';\nexport interface First {{}}";
        var second = $"{FileHeader.GeneratedNotice}\nimport {{ SecondDependency }} from './SecondDependency';\nexport interface Second {{}}";
        _result = FileHeader.Format("Application\\n{fileName}", "Combined.ts") + TypeScriptContentCombiner.Combine([first, second]);
    }

    [Fact] void should_keep_the_first_import() => _result.ShouldContain("import { FirstDependency } from './FirstDependency';");
    [Fact] void should_keep_the_second_import() => _result.ShouldContain("import { SecondDependency } from './SecondDependency';");
    [Fact] void should_keep_both_types() => _result.ShouldContain("export interface Second {}");
    [Fact] void should_emit_the_application_header_once() => _result.Split("// Application").Length.ShouldEqual(2);
    [Fact] void should_use_the_combined_file_name() => _result.ShouldContain("// Combined.ts");
}
