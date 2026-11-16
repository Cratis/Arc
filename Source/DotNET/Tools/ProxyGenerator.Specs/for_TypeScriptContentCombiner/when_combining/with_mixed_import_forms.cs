// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeScriptContentCombiner.when_combining;

public class with_mixed_import_forms : Specification
{
    string _result = null!;

    void Because() => _result = TypeScriptContentCombiner.Combine([
        "// First header\n/* eslint-disable first */\nimport './setup';\nimport Default from './default';\nimport { Zed, Alpha } from './shared';\nimport { Local } from './local';\nexport class First {}",
        "// Ignored header\n/* eslint-disable first */\n/* eslint-disable second */\nimport * as Other from './other';\nimport './setup';\nimport { Alpha, Beta as A } from './shared';\nimport { Local, External } from './mixed';\nexport class Local {}"
    ]);

    [Fact] void should_preserve_the_exact_combined_output() => _result.ShouldEqual(string.Join(Environment.NewLine,
        "// First header",
        "/* eslint-disable first */",
        "/* eslint-disable second */",
        "import { Beta as A, Alpha, Zed } from './shared';",
        "import { External, Local } from './mixed';",
        "import './setup';",
        "import Default from './default';",
        "import * as Other from './other';",
        string.Empty,
        "export class First {}",
        string.Empty,
        "export class Local {}",
        string.Empty));
}
