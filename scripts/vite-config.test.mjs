// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { mkdtemp, mkdir, writeFile, rm } from 'node:fs/promises';
import { resolve } from 'node:path';
import { test } from 'node:test';
import { build, loadConfigFromFile } from 'vite';

test('shared ESM config resolves a real tsconfig alias without scanning backend configs', async () => {
    await mkdir(resolve('.ai-work'), { recursive: true });
    const root = await mkdtemp(resolve('.ai-work/vite-alias-'));
    try {
        await mkdir(resolve(root, 'source'));
        await writeFile(resolve(root, 'tsconfig.json'), JSON.stringify({ compilerOptions: {
            baseUrl: '.', paths: { '@fixture/*': ['source/*'] }
        } }));
        await writeFile(resolve(root, 'source/value.ts'), 'export const value = "native-alias-resolved";');
        await writeFile(resolve(root, 'entry.ts'), 'import { value } from "@fixture/value"; console.log(value);');
        const loaded = await loadConfigFromFile({ command: 'build', mode: 'test' }, resolve('Source/JavaScript/Arc/vite.config.mts'));
        assert.ok(loaded);
        const config = loaded.config;
        assert.equal(config.resolve.tsconfigPaths, true);
        assert.equal(config.build.target, 'esnext');
        assert.equal('esbuild' in config, false);
        const result = await build({ ...config, root, configFile: false,
            build: { ...config.build, write: false, rolldownOptions: { input: resolve(root, 'entry.ts') } }
        });
        assert.ok(!Array.isArray(result) && 'output' in result);
        assert.ok(result.output.some(output => output.type === 'chunk' && output.code.includes('native-alias-resolved')));
    } finally {
        await rm(root, { recursive: true, force: true });
    }
});

test('every affected package loads the shared configuration as ESM', async () => {
    for (const name of ['Arc', 'Arc.React', 'Arc.React.MVVM', 'Arc.Vite']) {
        const loaded = await loadConfigFromFile({ command: 'serve', mode: 'test' }, resolve(`Source/JavaScript/${name}/vite.config.mts`));
        assert.ok(loaded, name);
        assert.equal(loaded.config.resolve.tsconfigPaths, true, name);
    }
});
