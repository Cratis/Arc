// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import { fileURLToPath } from 'node:url';

export default defineConfig({
    root: fileURLToPath(new URL('./', import.meta.url)),
    plugins: [react()],
    test: {
        globals: true,
        environment: 'jsdom',
        // The application is served from this path, and resolves its API calls relative to it.
        environmentOptions: { jsdom: { url: 'http://localhost/.cratis/event-model/' } },
        include: ['Frontend/**/for_*/**/when_*.tsx', 'Frontend/**/for_*/**/when_*.ts'],
        setupFiles: ['Frontend/Specs/setup.ts'],
        css: false
    }
});
