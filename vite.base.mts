// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import commonjs from 'vite-plugin-commonjs';
import { fileURLToPath } from 'node:url';

export function createConfig() {
    return {
        optimizeDeps: {
            exclude: ['tslib'],
        },
        build: { target: 'esnext' },
        resolve: { tsconfigPaths: true },
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
            setupFiles: fileURLToPath(new URL('./vitest.setup.ts', import.meta.url))
        },
        plugins: [
            commonjs()
        ]
    };
}
