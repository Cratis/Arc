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

/**
 * Represents an exported component to wrap at its declaration.
 * @remarks
 * An `expression` is wrapped where it stands: the initializer of a variable or the expression of a default export.
 * A `function` declaration is turned into a constant holding the wrapped function, since a declaration cannot be
 * wrapped in place.
 */
export type ComponentDeclaration =
    { kind: 'expression', node: AstNode, name?: string } |
    { kind: 'function', node: AstNode, name?: string, exportStatement?: AstNode };

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

const isClass = (node: AstNode) => node.type === 'ClassDeclaration' || node.type === 'ClassExpression';

const startsWithUppercase = (name: string) => /^[A-Z]/.test(name);

const elementFactories = /^_?(jsx|jsxs|jsxDEV|createElement)$/;

const transparentWrappers = ['ParenthesizedExpression', 'TSAsExpression', 'TSSatisfiesExpression', 'TSNonNullExpression', 'TSTypeAssertion'];

const unwrap = (node: AstNode): AstNode => transparentWrappers.includes(node.type) ? unwrap(node.expression as AstNode) : node;

const nameOf = (node: AstNode | null | undefined) => node?.type === 'Identifier' ? node.name as string : undefined;

type Rendered = 'element' | 'nothing' | 'other';

/**
 * Collects the nodes of a function body that belong to the function itself, leaving out nested functions and classes.
 * @param node The node to search.
 * @param visit The callback for every node of the function itself.
 */
function walkOwnNodes(node: AstNode, visit: (node: AstNode) => void): void {
    for (const child of childrenOf(node)) {
        if (isFunction(child) || isClass(child)) {
            continue;
        }
        visit(child);
        walkOwnNodes(child, visit);
    }
}

/**
 * Classifies what an expression a function returns renders.
 * @param expression The returned expression.
 * @param locals Constants declared in the function itself, by name.
 * @param depth Guards against constants referring to each other.
 * @returns `element` for JSX, `nothing` for null or undefined, `other` for anything else.
 */
function classify(expression: AstNode, locals: Map<string, AstNode>, depth: number = 0): Rendered {
    const node = unwrap(expression);
    switch (node.type) {
        case 'JSXElement':
        case 'JSXFragment':
            return 'element';

        case 'Literal':
            return node.value === null && node.raw === 'null' ? 'nothing' : 'other';

        case 'Identifier': {
            if (node.name === 'undefined') {
                return 'nothing';
            }
            const local = locals.get(node.name as string);
            return local && depth < 8 ? classify(local, locals, depth + 1) : 'other';
        }

        case 'ConditionalExpression': {
            const consequent = classify(node.consequent as AstNode, locals, depth);
            const alternate = classify(node.alternate as AstNode, locals, depth);
            if (consequent === 'other' || alternate === 'other') {
                return 'other';
            }
            return consequent === 'element' || alternate === 'element' ? 'element' : 'nothing';
        }

        case 'LogicalExpression':
            return classify(node.right as AstNode, locals, depth);

        case 'CallExpression': {
            const callee = unwrap(node.callee as AstNode);
            const name = callee.type === 'Identifier' ? callee.name as string
                : callee.type === 'MemberExpression' ? nameOf(callee.property as AstNode) : undefined;
            return name && elementFactories.test(name) ? 'element' : 'other';
        }

        default:
            return 'other';
    }
}

/**
 * Checks whether a function is a component, judged only by what the function itself returns.
 * @remarks
 * Every value the function returns must be JSX, null or undefined, possibly through conditional and logical
 * expressions or constants declared in the function itself, and at least one must be JSX. Functions nested in it,
 * such as callbacks or render props, are never looked into, so a function returning an object or array holding
 * functions that render JSX is not a component.
 * @param fn The function to check.
 * @returns True when the function is a component.
 */
export function isComponentFunction(fn: AstNode): boolean {
    if (fn.async === true || fn.generator === true) {
        return false;
    }

    const body = fn.body as AstNode | null;
    if (!body) {
        return false;
    }

    if (body.type !== 'BlockStatement') {
        return classify(body, new Map()) === 'element';
    }

    const locals = new Map<string, AstNode>();
    const returned: Rendered[] = [];
    walkOwnNodes(body, node => {
        if (node.type === 'VariableDeclarator' && node.init && (node.id as AstNode).type === 'Identifier') {
            locals.set((node.id as AstNode).name as string, node.init as AstNode);
        }
    });
    walkOwnNodes(body, node => {
        if (node.type === 'ReturnStatement') {
            returned.push(node.argument ? classify(node.argument as AstNode, locals) : 'nothing');
        }
    });

    return returned.includes('element') && !returned.includes('other');
}

/**
 * Checks whether an expression is or produces a component: a component function, or a call such as `memo(...)`,
 * `forwardRef(...)` or `withViewModel(...)` given one, directly or by the name of a top-level component.
 * @param expression The expression to check.
 * @param isTopLevelComponent Checks whether a name refers to a top-level component of the module.
 * @returns True when the expression is or produces a component.
 */
function producesComponent(expression: AstNode | null | undefined, isTopLevelComponent: (name: string) => boolean): boolean {
    if (!expression) {
        return false;
    }
    const node = unwrap(expression);
    if (isFunction(node)) {
        return isComponentFunction(node);
    }
    if (node.type === 'CallExpression') {
        return (node.arguments as AstNode[]).some(argument =>
            argument.type === 'Identifier' ? isTopLevelComponent(argument.name as string) : producesComponent(argument, isTopLevelComponent));
    }
    return false;
}

interface TopLevelDeclaration {
    declaration: ComponentDeclaration;
    isComponent: boolean;
}

/**
 * Gets the functions and variables declared at the top level of a module, by name.
 * @param body The statements of the module.
 * @returns The declarations by name.
 */
function topLevelDeclarationsOf(body: AstNode[]): Map<string, TopLevelDeclaration> {
    const declarations = new Map<string, TopLevelDeclaration>();
    const isTopLevelComponent = (name: string) => declarations.get(name)?.isComponent === true;

    for (const statement of body) {
        const exportStatement = statement.type === 'ExportNamedDeclaration' ? statement : undefined;
        const declaration = exportStatement ? exportStatement.declaration as AstNode | null : statement;
        if (!declaration) {
            continue;
        }
        if (declaration.type === 'FunctionDeclaration' && declaration.id) {
            const name = nameOf(declaration.id as AstNode)!;
            declarations.set(name, {
                declaration: { kind: 'function', node: declaration, name, exportStatement },
                isComponent: isComponentFunction(declaration)
            });
        }
        if (declaration.type === 'VariableDeclaration') {
            for (const declarator of declaration.declarations as AstNode[]) {
                const name = nameOf(declarator.id as AstNode);
                const init = declarator.init as AstNode | null;
                if (name && init) {
                    declarations.set(name, {
                        declaration: { kind: 'expression', node: init, name },
                        isComponent: producesComponent(init, isTopLevelComponent)
                    });
                }
            }
        }
    }
    return declarations;
}

/**
 * Gets the components a module exports, as the declarations to wrap. Named exports must start with an uppercase
 * letter; default exports may have any name or none.
 * @param program The parsed module.
 * @returns The component declarations, each once.
 */
export function componentsExportedFrom(program: AstNode): ComponentDeclaration[] {
    const body = program.body as AstNode[];
    const topLevel = topLevelDeclarationsOf(body);
    const isTopLevelComponent = (name: string) => topLevel.get(name)?.isComponent === true;
    const exported = new Set<ComponentDeclaration>();
    const exportByName = (name: string, mustBeUppercase: boolean) => {
        const found = topLevel.get(name);
        if (found?.isComponent && (!mustBeUppercase || startsWithUppercase(name))) {
            exported.add(found.declaration);
        }
    };

    for (const statement of body) {
        if (statement.type === 'ExportNamedDeclaration' && !statement.source) {
            const declaration = statement.declaration as AstNode | null;
            if (declaration?.type === 'FunctionDeclaration' && declaration.id) {
                exportByName(nameOf(declaration.id as AstNode)!, true);
            } else if (declaration?.type === 'VariableDeclaration') {
                for (const declarator of declaration.declarations as AstNode[]) {
                    const name = nameOf(declarator.id as AstNode);
                    if (name) exportByName(name, true);
                }
            } else if (!declaration) {
                for (const specifier of statement.specifiers as AstNode[]) {
                    const exportedAs = specifier.exported as AstNode;
                    const local = nameOf(specifier.local as AstNode)!;
                    const name = nameOf(exportedAs) ?? local;
                    if (name === 'default' || startsWithUppercase(name)) exportByName(local, false);
                }
            }
        }

        if (statement.type === 'ExportDefaultDeclaration') {
            const declaration = statement.declaration as AstNode;
            if (declaration.type === 'Identifier') {
                exportByName(declaration.name as string, false);
            } else if (declaration.type === 'FunctionDeclaration') {
                if (isComponentFunction(declaration)) {
                    exported.add({ kind: 'function', node: declaration, name: nameOf(declaration.id as AstNode | null), exportStatement: statement });
                }
            } else if (producesComponent(declaration, isTopLevelComponent)) {
                exported.add({ kind: 'expression', node: declaration });
            }
        }
    }

    return [...exported];
}
