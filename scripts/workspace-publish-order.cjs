// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

function getPublishPlan(packages) {
    const publishable = new Map([...packages].filter(([, manifest]) => manifest.private !== true));
    const dependencies = new Map();
    for (const [name, manifest] of publishable) {
        const internalDependencies = new Set();
        for (const field of ['dependencies', 'peerDependencies', 'optionalDependencies']) {
            for (const dependency of Object.keys(manifest[field] ?? {})) {
                if (publishable.has(dependency)) internalDependencies.add(dependency);
            }
        }
        dependencies.set(name, [...internalDependencies]);
    }

    const names = [];
    const visited = new Set();
    const visiting = [];
    function visit(name) {
        if (visited.has(name)) return;
        if (visiting.includes(name)) {
            const cycle = [...visiting.slice(visiting.indexOf(name)), name];
            throw new Error(`Workspace publish dependency cycle: ${cycle.join(' -> ')}`);
        }
        visiting.push(name);
        for (const dependency of dependencies.get(name)) visit(dependency);
        visiting.pop();
        visited.add(name);
        names.push(name);
    }
    for (const name of publishable.keys()) visit(name);
    // Keep private workspaces in the runner so they retain their existing skip behavior.
    for (const [name, manifest] of packages) {
        if (manifest.private === true) names.push(name);
    }
    return { names, dependencies };
}

module.exports = getPublishPlan;
