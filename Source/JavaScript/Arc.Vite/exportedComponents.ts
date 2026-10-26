// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Represents a node in an ESTree compatible syntax tree, as produced by the oxc parser.
 */
export interface AstNode {
    type: string;
    start: number;
    end: number;
    [key: string]: unknown;
}

const isNode = (value: unknown): value is AstNode =>
    typeof value === 'object' && value !== null && typeof (value as AstNode).type === 'string';

/**
 * Gets the direct child nodes of a node.
 * @param node The node to get the children of.
 * @returns The child nodes.
 */
export function childrenOf(node: AstNode): AstNode[] {
    return Object.entries(node)
        .filter(([key]) => key !== 'parent')
        .flatMap(([, value]) => Array.isArray(value) ? value.filter(isNode) : isNode(value) ? [value] : []);
}

/**
 * Checks whether a node is a function.
 * @param node The node to check.
 * @returns True for function declarations, function expressions and arrow functions.
 */
export const isFunction = (node: AstNode) =>
    node.type === 'FunctionDeclaration' || node.type === 'FunctionExpression' || node.type === 'ArrowFunctionExpression';

const startsWithUppercase = (name: string) => /^[A-Z]/.test(name);

const elementFactories = /^_?(jsx|jsxs|jsxDEV|createElement)$/;

/**
 * Checks whether a node renders JSX, either as JSX syntax or as already compiled element factory calls.
 * @param node The node to check.
 * @returns True when JSX is rendered anywhere within the node.
 */
function rendersJsx(node: AstNode): boolean {
    if (node.type === 'JSXElement' || node.type === 'JSXFragment') {
        return true;
    }
    if (node.type === 'CallExpression') {
        const callee = node.callee as AstNode;
        const name = callee.type === 'Identifier' ? callee.name as string
            : callee.type === 'MemberExpression' ? ((callee.property as AstNode).name as string | undefined) : undefined;
        if (name && elementFactories.test(name)) {
            return true;
        }
    }
    return childrenOf(node).some(rendersJsx);
}

const wrappers = ['ParenthesizedExpression', 'TSAsExpression', 'TSSatisfiesExpression', 'TSNonNullExpression'];

/**
 * Gets the functions an expression is or passes on: the function itself, or the functions given to a call such as
 * `memo(...)`, `forwardRef(...)` or `withViewModel(...)`.
 * @param expression The expression.
 * @returns The functions.
 */
function functionsOf(expression: AstNode | null | undefined): AstNode[] {
    if (!expression) {
        return [];
    }
    if (isFunction(expression)) {
        return [expression];
    }
    if (wrappers.includes(expression.type)) {
        return functionsOf(expression.expression as AstNode);
    }
    if (expression.type === 'CallExpression') {
        return (expression.arguments as AstNode[]).flatMap(functionsOf);
    }
    return [];
}

/**
 * Gets the functions declared at the top level of a module by name.
 * @param body The statements of the module.
 * @returns The functions by declared name.
 */
function topLevelFunctionsOf(body: AstNode[]): Map<string, AstNode[]> {
    const functions = new Map<string, AstNode[]>();
    for (const statement of body) {
        const declaration = statement.type === 'ExportNamedDeclaration' || statement.type === 'ExportDefaultDeclaration'
            ? statement.declaration as AstNode | null
            : statement;
        if (!declaration) {
            continue;
        }
        if (declaration.type === 'FunctionDeclaration' && declaration.id) {
            functions.set((declaration.id as AstNode).name as string, [declaration]);
        }
        if (declaration.type === 'VariableDeclaration') {
            for (const declarator of declaration.declarations as AstNode[]) {
                const id = declarator.id as AstNode;
                if (id.type === 'Identifier') {
                    functions.set(id.name as string, functionsOf(declarator.init as AstNode | null));
                }
            }
        }
    }
    return functions;
}

/**
 * Gets the components a module exports: exported functions that render JSX. Named exports must start with an
 * uppercase letter; default exports may have any name or none.
 * @param program The parsed module.
 * @returns The component functions, each once.
 */
export function componentsExportedFrom(program: AstNode): AstNode[] {
    const body = program.body as AstNode[];
    const topLevel = topLevelFunctionsOf(body);
    const exported: AstNode[] = [];
    const byName = (name: string) => topLevel.get(name) ?? [];

    for (const statement of body) {
        if (statement.type === 'ExportNamedDeclaration' && !statement.source) {
            const declaration = statement.declaration as AstNode | null;
            if (declaration?.type === 'FunctionDeclaration' && declaration.id) {
                const name = (declaration.id as AstNode).name as string;
                if (startsWithUppercase(name)) exported.push(declaration);
            } else if (declaration?.type === 'VariableDeclaration') {
                for (const declarator of declaration.declarations as AstNode[]) {
                    const id = declarator.id as AstNode;
                    if (id.type === 'Identifier' && startsWithUppercase(id.name as string)) {
                        exported.push(...functionsOf(declarator.init as AstNode | null));
                    }
                }
            } else if (!declaration) {
                for (const specifier of statement.specifiers as AstNode[]) {
                    const exportedAs = specifier.exported as AstNode;
                    const local = (specifier.local as AstNode).name as string;
                    const name = exportedAs.type === 'Identifier' ? exportedAs.name as string : local;
                    if (name === 'default' || startsWithUppercase(name)) exported.push(...byName(local));
                }
            }
        }

        if (statement.type === 'ExportDefaultDeclaration') {
            const declaration = statement.declaration as AstNode;
            exported.push(...(declaration.type === 'Identifier' ? byName(declaration.name as string) : functionsOf(declaration)));
        }
    }

    return [...new Set(exported)].filter(rendersJsx);
}
