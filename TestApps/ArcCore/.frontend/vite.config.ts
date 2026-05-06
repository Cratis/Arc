// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { fileURLToPath } from 'node:url';

const arc = (path: string) => fileURLToPath(new URL(`../../../Source/JavaScript/Arc/${path}`, import.meta.url));
const arcReact = (path: string) => fileURLToPath(new URL(`../../../Source/JavaScript/Arc.React/${path}`, import.meta.url));

export default defineConfig({
    root: fileURLToPath(new URL('../', import.meta.url)),
    build: {
        outDir: 'wwwroot',
        assetsDir: '',
    },
    plugins: [react()],
    resolve: {
        // Force a single copy of @cratis/fundamentals so that the Guid class reference
        // used as a key in JsonSerializer's typeConverters map is identical to the one
        // instantiated by command proxies. Without this, the Arc source node_modules
        // (7.7.3) and the workspace root node_modules (7.8.2) each produce a distinct
        // Guid class, causing Map.has() to always return false and Guid values to be
        // serialized as { bytes: [...], _stringVersion: "..." } instead of a string.
        dedupe: ['@cratis/fundamentals'],
        alias: [
            { find: '@cratis/arc/commands', replacement: arc('commands/index.ts') },
            { find: '@cratis/arc/identity', replacement: arc('identity/index.ts') },
            { find: '@cratis/arc/queries', replacement: arc('queries/index.ts') },
            { find: '@cratis/arc/validation', replacement: arc('validation/index.ts') },
            { find: '@cratis/arc/reflection', replacement: arc('reflection/index.ts') },
            { find: '@cratis/arc', replacement: arc('index.ts') },
            { find: '@cratis/arc.react/commands', replacement: arcReact('commands/index.ts') },
            { find: '@cratis/arc.react/dialogs', replacement: arcReact('dialogs/index.ts') },
            { find: '@cratis/arc.react/identity', replacement: arcReact('identity/index.ts') },
            { find: '@cratis/arc.react/queries', replacement: arcReact('queries/index.ts') },
            { find: '@cratis/arc.react', replacement: arcReact('index.ts') },
        ],
    },
    server: {
        proxy: {
            '/api': {
                target: 'http://localhost:5001',
                ws: true,
            },
            '/.cratis': {
                target: 'http://localhost:5001',
                ws: true,
            },
        },
    },
});
