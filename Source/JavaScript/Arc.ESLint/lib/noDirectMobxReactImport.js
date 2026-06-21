const FORBIDDEN_MODULES = new Set(['mobx-react', 'mobx-react-lite']);

// Flags direct imports from 'mobx-react' or 'mobx-react-lite'. In a Cratis Arc application the
// MobX binding is an internal detail of @cratis/arc.react.mvvm: 'observer' (the sanctioned leaf
// observer boundary) is re-exported from there, so consumers should never reach for the underlying
// MobX React packages directly. Importing 'observer' through @cratis/arc.react.mvvm lets the binding
// evolve without breaking consumers and keeps the sanctioned boundary discoverable.
export const noDirectMobxReactImport = {
    meta: {
        type: 'problem',
        docs: {
            description: "Disallow direct imports from 'mobx-react' or 'mobx-react-lite'; import 'observer' from '@cratis/arc.react.mvvm' instead.",
            recommended: true,
            url: 'https://github.com/Cratis/Arc/blob/main/Source/JavaScript/Arc.ESLint/README.md',
        },
        schema: [],
        messages: {
            noDirectImport: "Do not import from '{{module}}'. Import 'observer' from '@cratis/arc.react.mvvm' instead — the MobX binding is an internal detail of Cratis Arc MVVM.",
        },
    },
    create(context) {
        return {
            ImportDeclaration(node) {
                const module = node.source.value;
                if (FORBIDDEN_MODULES.has(module)) {
                    context.report({ node: node.source, messageId: 'noDirectImport', data: { module } });
                }
            },
        };
    },
};

export default noDirectMobxReactImport;
