// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeScriptContentCombiner.when_combining;

public class with_unsorted_import_members : Specification
{
    const string Header = "// Copyright (c) Cratis. All rights reserved.\n// Licensed under the MIT license. See LICENSE file in the project root for full license information.\n\n";
    string _result = null!;

    void Because() => _result = TypeScriptContentCombiner.Combine([
        Header + "import { Zebra, Alpha } from './z-first';\nimport { Beta } from './a-second';\nexport class First {}",
        Header + "import { Middle, Alpha } from './z-first';\nexport class Second {}"
    ]);

    [Fact] void should_sort_and_deduplicate_members() => _result.ShouldContain("import { Alpha, Middle, Zebra } from './z-first';");
    [Fact] void should_preserve_module_encounter_order() => _result.IndexOf("from './z-first'", StringComparison.Ordinal).ShouldBeLessThan(_result.IndexOf("from './a-second'", StringComparison.Ordinal));
    [Fact] void should_preserve_declaration_order() => _result.IndexOf("export class First", StringComparison.Ordinal).ShouldBeLessThan(_result.IndexOf("export class Second", StringComparison.Ordinal));
}
