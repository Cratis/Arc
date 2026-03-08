// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import commonjs from 'vite-plugin-commonjs';
import tsconfigPaths from 'vite-tsconfig-paths';

export function createConfig() {
    return {
        optimizeDeps: {
            exclude: ['tslib'],
        },
        esbuild: {
            supported: {
                'top-level-await': true,
            },
        },
        test: {
            globals: true,
            environment: 'node',
            sourcemap: false,
            isolate: false,
            fileParallelism: false,
            pool: 'threads',
            mock: {
                exclude: ['**/node_modules/**', 'node_modules/**'],
            },
            coverage: {
                exclude: [
                    '**/for_*/**',
                    '**/wwwroot/**',
                    '**/api/**',
                    '**/Api/**',
                    '**/dist/**',
                    '**/*.test.tsx',
                    '**/*.d.ts',
                    '**/declarations.ts',
                ],
            },
            exclude: ['**/dist/**', '**/node_modules/**', 'node_modules/**', '**/wwwroot/**', 'wwwroot/**', '**/given/**'],
            include: ['**/for_*/when_*/**/*.ts', '**/for_*/**/when_*.ts'],
            setupFiles: `${__dirname}/vitest.setup.ts`
        },
        plugins: [
            commonjs(),
            tsconfigPaths()
        ]
    };
}
