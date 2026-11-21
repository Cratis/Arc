// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const { spawnSync } = require('node:child_process');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { checkDeclarationPackages } = require('./declaration-guard-paths.cjs');

const rootOption = ['--root', '--repo-root'].includes(process.argv[2]) && process.argv.length === 4 && process.argv[3] !== '';
if (process.argv.length > 2 && !rootOption) {
    console.error('Usage: node scripts/check-declaration-consumers.cjs [--root <path> | --repo-root <path>]');
    process.exit(2);
}
const toolsRoot = path.resolve(__dirname, '..');
const root = rootOption ? path.resolve(process.argv[3]) : toolsRoot;
let checked;
try {
    checked = checkDeclarationPackages(root, ['Arc', 'Arc.React', 'Arc.React.MVVM', 'Arc.Vite']);
} catch (error) {
    console.error(error.message);
    process.exit(2);
}
const { checkedPath } = checked;

const fixtures = fs.mkdtempSync(path.join(os.tmpdir(), 'declaration-consumers-'));
const cleanup = () => fs.rmSync(fixtures, { recursive: true, force: true });
process.on('exit', cleanup);
for (const [signal, status] of [['SIGINT', 130], ['SIGTERM', 143]]) {
    process.on(signal, () => {
        cleanup();
        process.exit(status);
    });
}
const modules = path.join(fixtures, 'node_modules');
fs.mkdirSync(path.join(modules, '@cratis'), { recursive: true });

// Resolve third-party types from the tools' own dependencies, never the checked packages' node_modules.
for (const name of fs.readdirSync(path.join(toolsRoot, 'node_modules'))) {
    if (name !== '@cratis') fs.symlinkSync(path.join(toolsRoot, 'node_modules', name), path.join(modules, name));
}
for (const name of fs.readdirSync(path.join(toolsRoot, 'node_modules/@cratis'))) {
    fs.symlinkSync(path.join(toolsRoot, 'node_modules/@cratis', name), path.join(modules, '@cratis', name));
}
for (const { folder } of checked.packages) {
    const target = path.join(modules, '@cratis', path.basename(folder).toLowerCase());
    // Replace only the links just created in this fresh directory, never files in the data tree.
    if (fs.lstatSync(target, { throwIfNoEntry: false })) fs.unlinkSync(target);
    try {
        fs.cpSync(folder, target, {
            recursive: true,
            dereference: false,
            filter: source => path.basename(source) !== 'node_modules' && Boolean(checkedPath(source))
        });
    } catch (error) {
        console.error(`Cannot copy checked package ${folder}: ${error.message}`);
        process.exit(2);
    }
}
for (const fixture of ['consumer.mts', 'consumer.cts']) {
    fs.copyFileSync(path.join(__dirname, 'fixtures/declaration-consumer', fixture), path.join(fixtures, fixture));
}
const configurations = [
    ['Node16 ESM', 'node16', 'node16', 'consumer.mts'],
    ['Node16 CommonJS', 'node16', 'node16', 'consumer.cts'],
    ['NodeNext ESM', 'nodenext', 'nodenext', 'consumer.mts'],
    ['NodeNext CommonJS', 'nodenext', 'nodenext', 'consumer.cts'],
    ['Bundler', 'preserve', 'bundler', 'consumer.mts']
];

for (const [label, module, resolution, fixture] of configurations) {
    const result = spawnSync(process.execPath, [path.join(toolsRoot, 'node_modules/typescript/bin/tsc'),
        '--preserveSymlinks', '--ignoreConfig', '--noEmit', '--skipLibCheck', '--target', 'es2022',
        '--jsx', 'react-jsx', '--esModuleInterop', '--module', module,
        '--moduleResolution', resolution, path.join(fixtures, fixture)], {
        cwd: fixtures,
        stdio: 'inherit'
    });
    if (result.error) throw result.error;
    if (result.status !== 0) {
        console.error(`${label} consumer type-check failed (exit ${result.status ?? result.signal})`);
        process.exit(result.status || 1);
    }
    console.log(`${label} consumer type-check passed`);
}
