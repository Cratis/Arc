// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import typescript2 from 'rollup-plugin-typescript2';
import commonjs from 'rollup-plugin-commonjs';
import peerDepsExternal from 'rollup-plugin-peer-deps-external';
import { writeFileSync, mkdirSync } from 'fs';
import { join } from 'path';

/**
 * Rollup plugin to generate package.json files in output directories
 * This ensures proper module resolution for both CJS and ESM formats
 */
function generatePackageJson(cjsPath, esmPath) {
    return {
        name: 'generate-package-json',
        buildEnd() {
            // Create CJS package.json
            const cjsDir = cjsPath;
            mkdirSync(cjsDir, { recursive: true });
            writeFileSync(
                join(cjsDir, 'package.json'),
                JSON.stringify({ type: 'commonjs' }, null, 2),
                'utf-8'
            );

            // Create ESM package.json
            const esmDir = esmPath;
            mkdirSync(esmDir, { recursive: true });
            writeFileSync(
                join(esmDir, 'package.json'),
                JSON.stringify({ type: 'module' }, null, 2),
                'utf-8'
            );

            console.log('\u2713 Generated package.json files for CJS and ESM outputs');
        }
    };
}

export function rollup(cjsPath, esmPath, tsconfigPath, pkg) {
    return {
        input: "index.ts",

        output: [
            {
                dir: cjsPath,
                format: "cjs",
                exports: "named",
                sourcemap: true,
                preserveModules: true,
                preserveModulesRoot: "."
            },
            {
                dir: esmPath,
                format: "es",
                exports: "named",
                sourcemap: true,
                preserveModules: true,
                preserveModulesRoot: "."
            }
        ],
        external: [
            ...Object.keys(pkg.dependencies || {}),
            ...Object.keys(pkg.peerDependencies || {}),
            /^@cratis\/arc/,
            'react',
            'react-dom',
        ],
        plugins: [
            peerDepsExternal(),
            commonjs({
                include: /node_modules/,
                esmExternals: true,
                namedExports: {
                    'react/jsx-runtime': ['tsx', 'jsx', 'jsxs'],
                },
            }),
            typescript2({
                exclude: "for_**/**/*",
                tsconfig: tsconfigPath,
                clean: true
            }),
            generatePackageJson(cjsPath, esmPath)
        ]
    };
}
