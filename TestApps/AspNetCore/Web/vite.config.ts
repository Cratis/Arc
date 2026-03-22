import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { resolve } from 'path';

export default defineConfig({
    plugins: [react()],
    resolve: {
        alias: {
            Features: resolve(__dirname, '../Features'),
        },
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
