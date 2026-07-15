// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Yarn's builtin `@yarnpkg/plugin-compat` unconditionally wraps every `typescript`
// dependency in a patch (`builtin<compat/typescript>`) that only adds Plug'n'Play
// awareness to the compiler. This repo uses `nodeLinker: node-modules`, so the patch
// gives no benefit here, and its embedded diffs assume every TypeScript >=5.7.1
// release ships the same `lib/_tsc.js` layout - an assumption TypeScript 7's native
// package breaks, which makes `yarn install` fail with an ENOENT before the patch is
// even applied. Strip the patch back off so the plain npm package installs instead.
module.exports = {
    name: 'plugin-skip-typescript-compat-patch',
    factory: require => {
        const { structUtils } = require('@yarnpkg/core');

        return {
            hooks: {
                reduceDependency: async dependency => {
                    if (structUtils.stringifyIdent(dependency) !== 'typescript') return dependency;
                    if (!dependency.range.includes('builtin<compat/typescript>')) return dependency;

                    const source = /^patch:([^#]+)#/.exec(dependency.range)?.[1];
                    if (!source) return dependency;

                    return structUtils.parseDescriptor(decodeURIComponent(source), true);
                },
            },
        };
    },
};
