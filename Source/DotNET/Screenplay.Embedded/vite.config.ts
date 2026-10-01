// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { fileURLToPath } from 'node:url';

// The viewer is served from inside the hosting assembly, under whatever PathBase the host happens to
// run on, so every asset reference has to be relative - base './' - and the application resolves its
// own API calls from document.baseURI rather than from an absolute prefix.
export default defineConfig({
    root: fileURLToPath(new URL('./', import.meta.url)),
    base: './',
    plugins: [react()],
    build: {
        // The .NET project embeds everything under wwwroot as manifest resources.
        outDir: 'wwwroot',
        emptyOutDir: true,
        target: 'esnext',
        modulePreload: false,
        cssCodeSplit: false,
        rollupOptions: {
            output: {
                manualChunks(id: string) {
                    if (!id.includes('node_modules')) return undefined;
                    if (id.includes('pixi.js')) return 'pixi';
                    if (id.includes('primereact') || id.includes('primeicons')) return 'primereact';
                    if (id.includes('/react-dom') || id.includes('/react/') || id.includes('/scheduler')) return 'react-vendor';
                    return undefined;
                }
            }
        }
    },
    resolve: {
        // The board, the scene and the component library must all bind to one React instance.
        dedupe: ['@cratis/fundamentals', 'react', 'react-dom', 'react/jsx-runtime', 'react/jsx-dev-runtime']
    },
    server: {
        port: 5290,
        open: false,
        proxy: {
            '/.cratis/event-model/hierarchy': { target: 'http://localhost:5000' },
            '/.cratis/event-model/documents': { target: 'http://localhost:5000' }
        }
    }
});
