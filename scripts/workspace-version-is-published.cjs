// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

function isVersionPublished(spawn, name, version, manifest, location) {
    const args = ['view', `${name}@${version}`, 'version'];
    // npm view does not apply publishConfig; otherwise use the same cwd and inherited
    // npm configuration/environment as npm publish (including scoped registries).
    if (manifest.publishConfig?.registry) args.push('--registry', manifest.publishConfig.registry);
    const result = spawn('npm', args, { cwd: location });
    // E404 means absent; all other failures are unknown. Neither should prevent
    // attempting publish, and only confirmation of the exact version satisfies it.
    return result.status === 0 && result.stdout?.toString().trim() === version;
}

module.exports = isVersionPublished;
