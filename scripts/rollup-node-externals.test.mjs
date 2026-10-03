// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createRequire } from 'node:module';
import config from '../Source/JavaScript/Arc.Vite/rollup.config.mjs';

test('the Node-hosted Vite plugin declares both built-in import forms external', () => {
    for (const module of ['fs', 'path', 'fs/promises', 'node:fs', 'node:path', 'node:fs/promises']) {
        assert.ok(config.external.includes(module), `${module} must be supplied by Node`);
    }
    assert.ok(config.external.includes('@swc/core'));
    assert.ok(!config.external.includes('./EmitMetadataPlugin.js'));
    assert.equal(config.output.length, 2);
});

test('both built module formats load the Node plugin and emit decorator metadata', async () => {
    const require = createRequire(import.meta.url);
    const esm = await import('../Source/JavaScript/Arc.Vite/dist/esm/index.js');
    const cjs = require('../Source/JavaScript/Arc.Vite/dist/cjs/index.js');
    for (const { EmitMetadataPlugin } of [esm, cjs]) {
        const plugin = EmitMetadataPlugin({ contentRegEx: /@/ });
        const result = plugin.transform('class Model { @field value!: string; }', 'Model.ts');
        assert.ok(result.code.includes('design:type'));
        assert.ok(result.code.includes('String'));
    }
});
