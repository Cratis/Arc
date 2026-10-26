// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { parseSync } from 'oxc-parser';
import MagicString from 'magic-string';
import { SliceLocationParts } from './resolveSliceLocation.js';
import { componentsExportedFrom, AstNode, isFunction, childrenOf } from './exportedComponents.js';

/**
 * Represents transformed code with its source map.
 */
export interface TransformedCode {
    code: string;
    map: ReturnType<MagicString['generateMap']>;
}

const wrapper = '__cratisWithSliceLocation';
const locationConstant = '__cratisSliceLocation';
const locationType = '__CratisSliceLocation';

/**
 * Collects the expressions a function returns, without looking into functions or classes nested in it.
 * @param node The node to search.
 * @param returned The list to add returned expressions to.
 */
function collectReturnedExpressions(node: AstNode, returned: AstNode[]): void {
    for (const child of childrenOf(node)) {
        if (isFunction(child) || child.type === 'ClassDeclaration' || child.type === 'ClassExpression') {
            continue;
        }
        if (child.type === 'ReturnStatement') {
            const argument = child.argument as AstNode | null;
            if (argument) {
                returned.push(argument);
            }
            continue;
        }
        collectReturnedExpressions(child, returned);
    }
}

/**
 * Gets the expressions a component function renders.
 * @param component The component function.
 * @returns The rendered expressions.
 */
function renderedExpressionsOf(component: AstNode): AstNode[] {
    const body = component.body as AstNode;
    if (body.type !== 'BlockStatement') {
        return [body];
    }
    const returned: AstNode[] = [];
    collectReturnedExpressions(body, returned);
    return returned;
}

/**
 * Gets where to put the slice location declarations: after any directives such as `'use client'`.
 * @param program The parsed program.
 * @returns The position to insert at.
 */
function insertionPointOf(program: AstNode): number {
    const body = program.body as AstNode[];
    const directives = body.filter((statement, index) => body.slice(0, index + 1).every(leading => typeof leading.directive === 'string'));
    return directives.length > 0 ? directives[directives.length - 1].end : 0;
}

/**
 * Makes what the exported components of a module render carry the slice location of its folder.
 * @remarks
 * Every expression an exported component returns is wrapped with `withSliceLocation` from `@cratis/arc.react/slices`,
 * so the subtree the component renders resolves the slice location of the folder through `useSliceContext()`.
 * Components are exported functions, or functions passed to an exported call such as `memo(...)` or
 * `withViewModel(...)`, that render JSX. Named exports must start with an uppercase letter.
 * @param code The source code of the module.
 * @param file The path of the module, used to pick the parser language.
 * @param location The slice location of the folder the module lives in.
 * @param importFrom The module to import `withSliceLocation` and `SliceLocation` from.
 * @returns The transformed code, or undefined when the module exports no components or cannot be parsed.
 */
export function addSliceLocation(code: string, file: string, location: SliceLocationParts, importFrom: string = '@cratis/arc.react/slices'): TransformedCode | undefined {
    if (code.includes(wrapper)) {
        return undefined;
    }

    const parsed = parseSync(file, code);
    if (parsed.errors.length > 0) {
        return undefined;
    }

    const program = parsed.program as unknown as AstNode;
    const rendered = componentsExportedFrom(program).flatMap(renderedExpressionsOf);
    if (rendered.length === 0) {
        return undefined;
    }

    const magic = new MagicString(code);
    for (const expression of rendered) {
        magic.appendLeft(expression.start, `${wrapper}(${locationConstant}, (`);
        magic.prependRight(expression.end, '))');
    }

    const parts = [location.module, location.feature, location.slice].map(part => JSON.stringify(part)).join(', ');
    const declarations =
        `import { withSliceLocation as ${wrapper}, SliceLocation as ${locationType} } from ${JSON.stringify(importFrom)};\n` +
        `const ${locationConstant} = new ${locationType}(${parts});\n`;

    const insertionPoint = insertionPointOf(program);
    magic.appendLeft(insertionPoint, insertionPoint === 0 ? declarations : `\n${declarations}`);

    return {
        code: magic.toString(),
        map: magic.generateMap({ source: file, includeContent: true, hires: true })
    };
}
