const DEFAULT_HOOK_PATTERN = /^use[A-Z]/;

// Flags React hook calls made inside an MVVM view model. In Cratis MVVM a view model is
// a plain, React-free class — constructed and unit-tested without React — and everything
// it needs from the React world (identity, navigation, query/command hooks) is injected
// as a Cratis abstraction, never obtained by calling a hook. A hook called from a class
// named `*ViewModel` is therefore always a mistake, and one that is otherwise invisible
// until the component misbehaves at runtime.
//
// Only bare-identifier calls (`useState(...)`) are flagged, never member calls
// (`this.useDefaults()`, `service.useCache()`), so a view-model method that merely starts
// with `use` is not a false positive. Scope is the nearest enclosing class, so a hook in
// a non-view-model class nested inside a view model is left alone.
export const noHooksInViewModel = {
    meta: {
        type: 'problem',
        docs: {
            description: 'Disallow React hook calls inside MVVM view models (classes named *ViewModel).',
            recommended: true,
            url: 'https://github.com/Cratis/Arc/blob/main/Source/JavaScript/Arc.ESLint/README.md',
        },
        schema: [{
            type: 'object',
            properties: {
                classSuffix: { type: 'string' },
                hookPattern: { type: 'string' },
                additionalHooks: { type: 'array', items: { type: 'string' } },
            },
            additionalProperties: false,
        }],
        messages: {
            noHook: "Do not call the React hook '{{hook}}' inside view model '{{viewModel}}'. A view model must be a plain, React-free class — inject the Cratis abstraction instead of calling a hook.",
        },
    },
    create(context) {
        const options = context.options[0] ?? {};
        const classSuffix = options.classSuffix ?? 'ViewModel';
        const hookPattern = options.hookPattern ? new RegExp(options.hookPattern) : DEFAULT_HOOK_PATTERN;
        const additionalHooks = new Set(options.additionalHooks ?? []);

        // Stack of enclosing classes; each entry is the view-model name, or null when the
        // class is not a view model. The top of the stack is the nearest enclosing class.
        const enclosingClasses = [];

        const enterClass = node => {
            const name = node.id && node.id.name;
            enclosingClasses.push(name && name.endsWith(classSuffix) ? name : null);
        };
        const exitClass = () => enclosingClasses.pop();
        const nearestViewModel = () => (enclosingClasses.length ? enclosingClasses[enclosingClasses.length - 1] : null);

        return {
            ClassDeclaration: enterClass,
            'ClassDeclaration:exit': exitClass,
            ClassExpression: enterClass,
            'ClassExpression:exit': exitClass,
            CallExpression(node) {
                const viewModel = nearestViewModel();
                if (!viewModel) return;
                if (node.callee.type !== 'Identifier') return;
                const hook = node.callee.name;
                if (hookPattern.test(hook) || additionalHooks.has(hook)) {
                    context.report({ node, messageId: 'noHook', data: { hook, viewModel } });
                }
            },
        };
    },
};

export default noHooksInViewModel;
