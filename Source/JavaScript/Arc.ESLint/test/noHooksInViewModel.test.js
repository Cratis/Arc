import { RuleTester } from 'eslint';
import { afterAll, describe, it } from 'vitest';
import { noHooksInViewModel } from '../lib/noHooksInViewModel.js';

RuleTester.afterAll = afterAll;
RuleTester.describe = describe;
RuleTester.it = it;
RuleTester.itOnly = it.only;

const ruleTester = new RuleTester({
    languageOptions: { ecmaVersion: 'latest', sourceType: 'module' },
});

ruleTester.run('no-hooks-in-view-model', noHooksInViewModel, {
    valid: [
        // A hook in a function component is exactly where it belongs.
        'function MyComponent() { const [value, setValue] = useState(0); return value; }',
        // A view model with no hooks.
        'class FooViewModel { constructor() { this.value = 1; } }',
        // A member call that merely starts with `use` is not a hook.
        'class FooViewModel { load() { return this.useDefaults(); } }',
        'class FooViewModel { load(service) { return service.useCache(); } }',
        // A non-view-model class is out of scope.
        'class Helper { run() { return useState(0); } }',
        // A hook inside a non-view-model class nested in a view model is left alone.
        'class FooViewModel { build() { return class Inner { run() { return useMemo(() => 1, []); } }; } }',
    ],
    invalid: [
        {
            code: 'class FooViewModel { constructor() { const [v, setV] = useState(0); } }',
            errors: [{ messageId: 'noHook', data: { hook: 'useState', viewModel: 'FooViewModel' } }],
        },
        {
            code: 'class BarViewModel { init() { useEffect(() => {}, []); } }',
            errors: [{ messageId: 'noHook', data: { hook: 'useEffect', viewModel: 'BarViewModel' } }],
        },
        {
            code: 'class BazViewModel { get current() { return useIdentity(); } }',
            errors: [{ messageId: 'noHook', data: { hook: 'useIdentity', viewModel: 'BazViewModel' } }],
        },
        {
            // Custom suffix option.
            code: 'class FooModel { run() { return useNavigate(); } }',
            options: [{ classSuffix: 'Model' }],
            errors: [{ messageId: 'noHook', data: { hook: 'useNavigate', viewModel: 'FooModel' } }],
        },
        {
            // additionalHooks catches a non use-prefixed injectable getter.
            code: 'class FooViewModel { run() { return inject(); } }',
            options: [{ additionalHooks: ['inject'] }],
            errors: [{ messageId: 'noHook', data: { hook: 'inject', viewModel: 'FooViewModel' } }],
        },
    ],
});
