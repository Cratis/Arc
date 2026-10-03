// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { readdir, stat } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { test } from 'node:test';

test('production viewer chunks and its bundled editor worker stay below the default 500 kB budget', async () => {
    const directory = new URL('../Source/DotNET/Screenplay.Embedded/wwwroot/assets/', import.meta.url);
    const files = (await readdir(directory)).filter(name => name.endsWith('.js'));
    assert.ok(files.length > 1, 'production build must exist and contain split chunks');
    assert.equal(files.filter(name => name.startsWith('editor.worker-')).length, 1,
        'the editor worker must remain a separate bundled asset');
    for (const name of files) {
        assert.ok((await stat(new URL(name, directory))).size <= 500_000,
            `${fileURLToPath(new URL(name, directory))} exceeds 500 kB`);
    }
    console.log(`Checked ${files.length} production JavaScript chunks including the editor worker`);
});
