// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { fileURLToPath } from 'node:url';
import { monacoAliases } from './monaco.aliases.ts';

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
        rolldownOptions: {
            output: {
                codeSplitting: {
                    // Capture dependency closures before their consumers. Arbitrary size splits of
                    // Monaco core or Pixi create cycles that fail during class initialization.
                    // Only independent editor contributions use size-bounded subdivision.
                    groups: [
                        { name: 'react-vendor', test: /node_modules[/](react|react-dom|scheduler)[/]/ },
                        { name: 'monaco-base-common', test: /monaco-editor\/esm\/vs\/base\/common\// },
                        { name: 'monaco-base-browser', test: /monaco-editor\/esm\/vs\/base\/browser\// },
                        { name: 'monaco-base', test: /monaco-editor\/esm\/vs\/base\// },
                        { name: 'monaco-platform', test: /monaco-editor\/esm\/vs\/platform\// },
                        { name: 'monaco-core', test: /monaco-editor\/esm\/vs\/editor\/common\/core\// },
                        { name: 'monaco-model', test: /monaco-editor\/esm\/vs\/editor\/common\/model\// },
                        { name: 'monaco-services', test: /monaco-editor\/esm\/vs\/editor\/common\/services\// },
                        { name: 'monaco-common', test: /monaco-editor\/esm\/vs\/editor\/common\// },
                        { name: 'monaco-view', test: /monaco-editor\/esm\/vs\/editor\/browser\/(view|viewParts)\// },
                        { name: 'monaco-browser', test: /monaco-editor\/esm\/vs\/editor\/browser\// },
                        { name: 'monaco-contrib', test: /monaco-editor\/esm\/vs\/editor\/contrib\//, maxSize: 450_000 },
                        { name: 'monaco', test: /node_modules[/]monaco-editor[/]/ },
                        { name: 'pixi', test: /node_modules[/]pixi.js[/]/ },
                        { name: 'screenplay-language-shared', test: /node_modules[/]@cratis[/]screenplay-language[/]dist[/]bundles[/]chunk-/ },
                        { name: 'screenplay-language-entry', test: /node_modules[/]@cratis[/]screenplay-language[/]dist[/]bundles[/](index|sub-languages[/].*)\.js$/ }
                    ]
                }
            }
        }
    },
    resolve: {
        alias: monacoAliases,
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
