import recommended, { plugin } from './recommended.js';
import { noHooksInViewModel } from './lib/noHooksInViewModel.js';
import { isGeneratedProxy, skipGeneratedProxies } from './lib/skipGeneratedProxies.js';

// Cratis Arc ESLint rules. Composes on top of @cratis/eslint-config.
//
//   configs.recommended  skip generated proxies + no-hooks-in-view-model
//   rules                the rule implementations, for custom wiring
//   processors           the skip-generated-proxies processor, for custom wiring
export const configs = { recommended };
export const rules = { 'no-hooks-in-view-model': noHooksInViewModel };
export const processors = { 'skip-generated-proxies': skipGeneratedProxies };

export { plugin, noHooksInViewModel, skipGeneratedProxies, isGeneratedProxy };

export default { configs, rules, processors, plugin };
