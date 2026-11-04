// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { mkdir, mkdtemp, rm, writeFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import { test } from 'node:test';
import { rollup } from 'rollup';
import { transformSync } from '@swc/core';
import { build } from 'vite';

// The slice-location generator spec supplies its actual emitted proxies on stdin.
let input = '';
for await (const chunk of process.stdin) input += chunk;
const fixtures = JSON.parse(input);
assert.equal(fixtures.length, 3, 'exercise command, query and observable query output');
assert.deepEqual(fixtures.map(fixture => fixture.Kind), ['command', 'query', 'observable query']);

for (const { Kind: kind, Name: name, Content: proxy } of fixtures) {
    test(`an unused generated ${kind} is removed when a sibling export is used`, async () => {
        await mkdir(resolve('.ai-work'), { recursive: true });
        const root = await mkdtemp(resolve('.ai-work/proxy-slice-location-'));
        try {
            assert.ok(proxy.includes(`export class ${name} extends `));
            assert.match(proxy, /static readonly sliceLocation: ArcSliceLocation/);
            const route = proxy.match(/readonly route: string = '([^']+)'/)[1];
            assert.ok(proxy.includes('unused-proxy-role'), 'the fixture must include role metadata');
            await writeFile(resolve(root, 'proxy.ts'), `${proxy}\nexport class Sibling { static value = 'retained-sibling'; }\n`);
            const entry = resolve(root, 'entry.ts');
            await writeFile(entry, "import { Sibling } from './proxy.ts'; console.log(Sibling.value);");
            const external = id => id.startsWith('@cratis/');
            const viteResult = await build({ root, configFile: false, logLevel: 'silent',
                build: { write: false, minify: false, target: 'esnext', rolldownOptions: { input: entry, external } }
            });
            assert.ok(!Array.isArray(viteResult) && 'output' in viteResult);
            const rollupBundle = await rollup({ input: entry, external, plugins: [{
                name: 'typescript',
                resolveId(source, importer) {
                    if (source.startsWith('.')) return resolve(importer, '..', source);
                },
                transform(code, id) {
                    return transformSync(code, { filename: id, jsc: { parser: { syntax: 'typescript' }, target: 'esnext' } });
                }
            }] });
            try {
                const rollupResult = await rollupBundle.generate({ format: 'es' });
                for (const [bundler, result] of [['Vite/rolldown', viteResult], ['Rollup', rollupResult]]) {
                    const code = result.output.filter(output => output.type === 'chunk').map(chunk => chunk.code).join('\n').replace(/^\/\/.*$/gm, '');
                    assert.ok(code.includes('retained-sibling'), `${bundler} must keep the used sibling`);
                    assert.ok(!code.includes(name), `${bundler} must remove the unused ${kind}:\n${code}`);
                    assert.ok(!code.includes(route), `${bundler} must remove the unused route`);
                    assert.ok(!code.includes('unused-proxy-role'), `${bundler} must remove the unused role`);
                    assert.ok(!code.includes('sliceLocation'), `${bundler} must remove the unused slice metadata`);
                    console.log(`${kind}, ${bundler}: kept sibling, removed proxy, route, role and slice location (${Buffer.byteLength(code)} bytes)`);
                }
            } finally {
                await rollupBundle.close();
            }
        } finally {
            await rm(root, { recursive: true, force: true });
        }
    });
}
