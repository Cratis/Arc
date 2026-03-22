import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { resolve } from 'path';

const arcRoot = resolve(__dirname, '../../Source/JavaScript/Arc');
const arcReactRoot = resolve(__dirname, '../../Source/JavaScript/Arc.React');

export default defineConfig({
    plugins: [react()],
    resolve: {
        alias: [
            // Resolve @cratis/arc sub-paths to source files directly (no build required)
            { find: '@cratis/arc.react/queries', replacement: resolve(arcReactRoot, 'queries/index.ts') },
            { find: '@cratis/arc.react/commands', replacement: resolve(arcReactRoot, 'commands/index.ts') },
            { find: '@cratis/arc.react/dialogs', replacement: resolve(arcReactRoot, 'dialogs/index.ts') },
            { find: '@cratis/arc.react/identity', replacement: resolve(arcReactRoot, 'identity/index.ts') },
            { find: '@cratis/arc.react', replacement: resolve(arcReactRoot, 'index.ts') },
            { find: '@cratis/arc/reflection', replacement: resolve(arcRoot, 'reflection/index.ts') },
            { find: '@cratis/arc/commands', replacement: resolve(arcRoot, 'commands/index.ts') },
            { find: '@cratis/arc/queries', replacement: resolve(arcRoot, 'queries/index.ts') },
            { find: '@cratis/arc/validation', replacement: resolve(arcRoot, 'validation/index.ts') },
            { find: '@cratis/arc/identity', replacement: resolve(arcRoot, 'identity/index.ts') },
            { find: '@cratis/arc', replacement: resolve(arcRoot, 'index.ts') },
            { find: 'Features', replacement: resolve(__dirname, '../Features') },
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
