import { createRequire } from 'node:module';
import { noHooksInViewModel } from './lib/noHooksInViewModel.js';
import { isGeneratedProxy, skipGeneratedProxies } from './lib/skipGeneratedProxies.js';

const { version } = createRequire(import.meta.url)('./package.json');

// A single flat-config plugin object — meta + rules + processors + self-referencing
// configs — per the ESLint flat-config plugin convention. The default export IS the
// plugin, so consumers get `arc.meta`, `arc.rules`, `arc.processors`, and `arc.configs`
// directly. Composes on top of @cratis/eslint-config.
const plugin = {
    meta: { name: '@cratis/eslint-plugin-arc', version },
    rules: { 'no-hooks-in-view-model': noHooksInViewModel },
    processors: { 'skip-generated-proxies': skipGeneratedProxies },
    configs: {},
};

// configs reference the plugin itself, so they are assigned after it exists.
//
//   import cratis from '@cratis/eslint-config';
//   import arc from '@cratis/eslint-plugin-arc';
//   export default [...cratis.configs.consumer, ...arc.configs.recommended];
Object.assign(plugin.configs, {
    recommended: [
        {
            name: '@cratis/arc/skip-generated-proxies',
            files: ['**/*.ts'],
            processor: plugin.processors['skip-generated-proxies'],
        },
        {
            name: '@cratis/arc/recommended',
            files: ['**/*.ts', '**/*.tsx'],
            plugins: { '@cratis/arc': plugin },
            rules: {
                '@cratis/arc/no-hooks-in-view-model': 'error',
            },
        },
    ],
});

export default plugin;
export const { configs, rules, processors, meta } = plugin;
export { noHooksInViewModel, skipGeneratedProxies, isGeneratedProxy };
