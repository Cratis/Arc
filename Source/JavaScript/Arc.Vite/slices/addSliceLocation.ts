// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { parseSync } from 'oxc-parser';
import MagicString from 'magic-string';
import { SliceLocationParts } from './resolveSliceLocation.js';
import { componentsExportedFrom, AstNode, ComponentDeclaration } from './exportedComponents.js';

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
 * Wraps one component declaration with `withSliceLocation`.
 * @remarks
 * The component goes first and the location second, so React Fast Refresh, which only looks into the first argument
 * of a call, still registers the component function inside the call.
 * @param magic The code being changed.
 * @param component The component declaration to wrap.
 */
function wrap(magic: MagicString, component: ComponentDeclaration): void {
    const { node } = component;
    const suffix = component.name ? `, ${locationConstant}, ${JSON.stringify(component.name)})` : `, ${locationConstant})`;

    if (component.kind === 'expression') {
        magic.appendLeft(node.start, `${wrapper}(`);
        magic.prependRight(node.end, suffix);
        return;
    }

    const exportStatement = component.exportStatement;
    if (exportStatement?.type === 'ExportDefaultDeclaration') {
        if (component.name) {
            magic.overwrite(exportStatement.start, node.start, `const ${component.name} = ${wrapper}(`);
            magic.prependRight(node.end, `${suffix};\nexport default ${component.name};`);
        } else {
            magic.overwrite(exportStatement.start, node.start, `export default ${wrapper}(`);
            magic.prependRight(node.end, `${suffix};`);
        }
        return;
    }

    magic.appendLeft(node.start, `const ${component.name} = ${wrapper}(`);
    magic.prependRight(node.end, `${suffix};`);
}

/**
 * Makes the exported components of a module render within a slice scope for the folder the module lives in.
 * @remarks
 * Every exported component is wrapped at its declaration with `withSliceLocation` from `@cratis/arc.react/slices`:
 * `export const Pay = __cratisWithSliceLocation(function Pay() { ... }, __cratisSliceLocation)`. The component itself,
 * and everything it renders, then resolves the folder location through `useSliceContext()`, and its own command and
 * query hooks register within the folder scope. Exported function declarations become constants, so they are no longer
 * hoisted. Components are judged by what they return; see `isComponentFunction`.
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
    const components = componentsExportedFrom(program);
    if (components.length === 0) {
        return undefined;
    }

    const magic = new MagicString(code);
    const parts = [location.module, location.feature, location.slice].map(part => JSON.stringify(part)).join(', ');
    const declarations =
        `import { withSliceLocation as ${wrapper}, SliceLocation as ${locationType} } from ${JSON.stringify(importFrom)};\n` +
        `const ${locationConstant} = new ${locationType}(${parts});\n`;

    const insertionPoint = insertionPointOf(program);
    magic.appendLeft(insertionPoint, insertionPoint === 0 ? declarations : `\n${declarations}`);

    for (const component of components) {
        wrap(magic, component);
    }

    return {
        code: magic.toString(),
        map: magic.generateMap({ source: file, includeContent: true, hires: true })
    };
}
