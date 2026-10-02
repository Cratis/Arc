// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const getPublishPlan = require('../workspace-publish-order.cjs');
const isVersionPublished = require('../workspace-version-is-published.cjs');

const root = path.resolve(__dirname, '../..');
const script = fs.readFileSync(path.join(root, 'run-task-on-workspaces.js'), 'utf8').replace(/^#!.*\n/, '');

// Execute the actual CLI against in-memory manifests; no install or real npm call is needed.
function runWorkspaces(packages, { task = 'publish-version', args = ['22.32.1'], results = {}, viewResults = {} } = {}) {
    const files = new Map([[path.join(root, 'README.md'), 'Root README']]);
    const packagePaths = packages.map((manifest, index) => {
        const location = path.join('packages', String(index), 'package.json');
        files.set(path.join(root, location), JSON.stringify({ version: '1.0.0', ...manifest }));
        return location;
    });
    const calls = [];
    const viewCalls = [];
    const output = [];
    const exitSignal = {};
    let status = 0;
    const read = location => files.get(path.resolve(root, location));
    const modules = {
        path,
        fs: {
            readFileSync: location => Buffer.from(read(location)),
            existsSync: location => files.has(path.resolve(root, location)),
            copyFileSync: (source, target) => files.set(target, read(source))
        },
        glob: { sync: () => packagePaths },
        './package.json': { workspaces: ['packages/*'] },
        './scripts/workspace-publish-order.cjs': getPublishPlan,
        './scripts/workspace-version-is-published.cjs': isVersionPublished,
        'edit-json-file': location => {
            const manifest = JSON.parse(read(location));
            return {
                toObject: () => manifest,
                get: field => manifest[field],
                set: (field, value) => { manifest[field] = value; },
                save: () => files.set(location, JSON.stringify(manifest))
            };
        },
        child_process: {
            spawnSync: (command, args, options) => {
                const manifest = JSON.parse(read(path.join(options.cwd, 'package.json')));
                const call = { command, args: Array.from(args), cwd: options.cwd, name: manifest.name, manifest };
                if (command === 'npm' && args[0] === 'view') {
                    const index = viewCalls.filter(previous => previous.name === manifest.name).length;
                    viewCalls.push(call);
                    const configured = viewResults[manifest.name];
                    const result = Array.isArray(configured) ? configured[index] : configured;
                    return { status: 1, stdout: Buffer.alloc(0), stderr: Buffer.from('npm error code E404'), ...result };
                }
                calls.push(call);
                return { status: 0, stdout: Buffer.alloc(0), stderr: Buffer.alloc(0), ...results[manifest.name] };
            }
        }
    };
    const execute = vm.runInNewContext(`(function (require) {\n${script}\n})`, {
        process: {
            argv: ['node', 'run-task-on-workspaces.js', task, ...args],
            cwd: () => root,
            exit: code => { status = code; throw exitSignal; }
        },
        console: {
            log: message => output.push(message),
            error: message => output.push(message)
        }
    });
    try {
        execute(name => {
            if (!(name in modules)) throw new Error(`Unexpected module: ${name}`);
            return modules[name];
        });
    } catch (error) {
        if (error !== exitSignal) throw error;
    }
    return { status, calls, viewCalls, output: output.join('\n'), manifests: packagePaths.map(location => JSON.parse(read(location))), files };
}

module.exports = runWorkspaces;
