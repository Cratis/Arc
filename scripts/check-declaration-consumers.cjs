// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const { spawnSync } = require('node:child_process');
const path = require('node:path');

const root = path.resolve(__dirname, '..');
const fixtures = path.join(__dirname, 'fixtures/declaration-consumer');
const configurations = [
    ['Node16 ESM', 'node16', 'node16', 'consumer.mts'],
    ['Node16 CommonJS', 'node16', 'node16', 'consumer.cts'],
    ['NodeNext ESM', 'nodenext', 'nodenext', 'consumer.mts'],
    ['NodeNext CommonJS', 'nodenext', 'nodenext', 'consumer.cts'],
    ['Bundler', 'preserve', 'bundler', 'consumer.mts']
];

for (const [label, module, resolution, fixture] of configurations) {
    const result = spawnSync('yarn', ['tsc', '--ignoreConfig', '--noEmit', '--skipLibCheck', '--target', 'es2022',
        '--jsx', 'react-jsx', '--esModuleInterop', '--module', module,
        '--moduleResolution', resolution, path.join(fixtures, fixture)], {
        cwd: root,
        stdio: 'inherit'
    });
    if (result.error) throw result.error;
    if (result.status !== 0) {
        console.error(`${label} consumer type-check failed (exit ${result.status ?? result.signal})`);
        process.exit(result.status || 1);
    }
    console.log(`${label} consumer type-check passed`);
}
