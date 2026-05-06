// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Builds the frontend with full identifier minification — class and function names
// are mangled to single letters, exactly as a production mobile/desktop bundler
// would do without keep_classnames / keep_fnames guards.
//
// Use `yarn serve:minified` to build and start a preview server (port 4173) that
// proxies /api and /.cratis to the Arc backend on localhost:5001.
// This lets you verify that Arc's queryName-based cache keys work correctly even
// when every class name in the bundle is a single letter.

import { defineConfig, mergeConfig } from 'vite';
import baseConfig from './vite.config';

export default mergeConfig(baseConfig, defineConfig({
    build: {
        outDir: 'dist-minified',
        assetsDir: '',
        minify: 'esbuild',
    },
    esbuild: {
        // Explicitly opt out of name preservation so the bundle behaves like a
        // production React Native / mobile build where every class becomes 'a', 'b', etc.
        keepNames: false,
    },
    preview: {
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
}));
