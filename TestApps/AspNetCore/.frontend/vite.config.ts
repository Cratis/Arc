import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { fileURLToPath } from 'node:url';

export default defineConfig({
    root: fileURLToPath(new URL('../', import.meta.url)),
    build: {
        outDir: 'wwwroot',
        assetsDir: '',
    },
    plugins: [react()],
    resolve: {
        alias: [
            // Resolve @cratis/arc sub-paths to source files directly (no build required)
            { find: '@cratis/arc.react/queries', replacement: fileURLToPath(new URL('../../Source/JavaScript/Arc.React/queries/index.ts', import.meta.url)) },
            { find: '@cratis/arc.react/commands', replacement: fileURLToPath(new URL('../../Source/JavaScript/Arc.React/commands/index.ts', import.meta.url)) },
            { find: '@cratis/arc.react/dialogs', replacement: fileURLToPath(new URL('../../Source/JavaScript/Arc.React/dialogs/index.ts', import.meta.url)) },
            { find: '@cratis/arc.react/identity', replacement: fileURLToPath(new URL('../../Source/JavaScript/Arc.React/identity/index.ts', import.meta.url)) },
            { find: '@cratis/arc.react', replacement: fileURLToPath(new URL('../../Source/JavaScript/Arc.React/index.ts', import.meta.url)) },
            { find: '@cratis/arc/reflection', replacement: fileURLToPath(new URL('../../Source/JavaScript/Arc/reflection/index.ts', import.meta.url)) },
            { find: '@cratis/arc/commands', replacement: fileURLToPath(new URL('../../Source/JavaScript/Arc/commands/index.ts', import.meta.url)) },
            { find: '@cratis/arc/queries', replacement: fileURLToPath(new URL('../../Source/JavaScript/Arc/queries/index.ts', import.meta.url)) },
            { find: '@cratis/arc/validation', replacement: fileURLToPath(new URL('../../Source/JavaScript/Arc/validation/index.ts', import.meta.url)) },
            { find: '@cratis/arc/identity', replacement: fileURLToPath(new URL('../../Source/JavaScript/Arc/identity/index.ts', import.meta.url)) },
            { find: '@cratis/arc', replacement: fileURLToPath(new URL('../../Source/JavaScript/Arc/index.ts', import.meta.url)) },
            { find: 'Features', replacement: fileURLToPath(new URL('../Features', import.meta.url)) },
        ],
    },
    server: {
        proxy: {
            '/api': 'http://localhost:5000',
            '/.cratis': {
                target: 'http://localhost:5000',
                ws: true,
            },
        },
    },
});
