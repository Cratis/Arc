// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Screens.for_ScreenImports.when_reading_imports;

public class and_double_slashes_sit_in_strings_and_templates : Specification
{
    const string Text = """
        import { A } from './A';
        const url = "//cdn";
        const template = `
        import { B } from './B';
        ${`nested${value}//text`}`;
        import { C } from './C'; // ignored
        """;

    IReadOnlyCollection<string> _imports;

    void Because() => _imports = ScreenImports.In(Text);

    [Fact] void should_read_only_real_imports() => _imports.ShouldContainOnly(["A", "C"]);
}
