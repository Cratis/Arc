import { createRequire } from 'node:module';
import { noHooksInViewModel } from './lib/noHooksInViewModel.js';
import { skipGeneratedProxies } from './lib/skipGeneratedProxies.js';

const { version } = createRequire(import.meta.url)('./package.json');

export const plugin = {
    meta: { name: '@cratis/eslint-plugin-arc', version },
    rules: { 'no-hooks-in-view-model': noHooksInViewModel },
    processors: { 'skip-generated-proxies': skipGeneratedProxies },
};

// Arc-specific flat config. Compose it AFTER the Cratis base
// (`@cratis/eslint-config`) in a consuming project:
//
//   import cratis from '@cratis/eslint-config';
//   import arc from '@cratis/eslint-plugin-arc';
//   export default [...cratis.configs.consumer, ...arc.configs.recommended];
const recommended = [
    {
        // Skip Cratis-generated Arc proxies wholesale. Keyed on the generated-file header,
        // because proxies sit intermixed with hand-written `.ts` under the same folders.
        files: ['**/*.ts'],
        processor: skipGeneratedProxies,
    },
    {
        files: ['**/*.ts', '**/*.tsx'],
        plugins: { '@cratis/arc': plugin },
        rules: {
            '@cratis/arc/no-hooks-in-view-model': 'error',
        },
    },
];

export default recommended;
