import { RuleTester } from 'eslint';
import { afterAll, describe, it } from 'vitest';
import { noDirectMobxReactImport } from '../lib/noDirectMobxReactImport.js';

RuleTester.afterAll = afterAll;
RuleTester.describe = describe;
RuleTester.it = it;
RuleTester.itOnly = it.only;

const ruleTester = new RuleTester({
    languageOptions: { ecmaVersion: 'latest', sourceType: 'module' },
});

ruleTester.run('no-direct-mobx-react-import', noDirectMobxReactImport, {
    valid: [
        "import { observer } from '@cratis/arc.react.mvvm';",
        "import { withViewModel } from '@cratis/arc.react.mvvm';",
        "import { makeAutoObservable } from 'mobx';",
        "import { observer } from 'some-other-mobx-react-wrapper';",
        "import React from 'react';",
    ],
    invalid: [
        {
            code: "import { observer } from 'mobx-react';",
            errors: [{ messageId: 'noDirectImport', data: { module: 'mobx-react' } }],
        },
        {
            code: "import { observer } from 'mobx-react-lite';",
            errors: [{ messageId: 'noDirectImport', data: { module: 'mobx-react-lite' } }],
        },
        {
            code: "import { Observer } from 'mobx-react';",
            errors: [{ messageId: 'noDirectImport', data: { module: 'mobx-react' } }],
        },
        {
            code: "import mobxReact from 'mobx-react';",
            errors: [{ messageId: 'noDirectImport', data: { module: 'mobx-react' } }],
        },
        {
            code: "import * as mobxReactLite from 'mobx-react-lite';",
            errors: [{ messageId: 'noDirectImport', data: { module: 'mobx-react-lite' } }],
        },
        {
            code: "import 'mobx-react';",
            errors: [{ messageId: 'noDirectImport', data: { module: 'mobx-react' } }],
        },
    ],
});
