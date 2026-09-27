// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { existsSync, readFileSync, readdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import ts from 'typescript-for-eslint';

const packageRoot = join(dirname(fileURLToPath(import.meta.url)), '..');
const manifest = JSON.parse(readFileSync(join(packageRoot, 'package.json'), 'utf-8')) as {
    name: string;
    dependencies?: Record<string, string>;
    peerDependencies?: Record<string, string>;
};

/** Specs are emitted by tsc, but their imports are not part of the package's runtime graph. */
const runtimeFiles = (directory: string): string[] => readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
    const path = join(directory, entry.name);
    if (entry.isDirectory()) return entry.name.startsWith('for_') ? [] : runtimeFiles(path);
    return entry.name.endsWith('.js') ? [path] : [];
});

/** Parse actual imports, not documentation comments that happen to contain import examples. */
const importsIn = (path: string): string[] => {
    const source = ts.createSourceFile(path, readFileSync(path, 'utf-8'), ts.ScriptTarget.Latest, true, ts.ScriptKind.JS);
    const imports: string[] = [];
    const visit = (node: ts.Node) => {
        if ((ts.isImportDeclaration(node) || ts.isExportDeclaration(node)) &&
            node.moduleSpecifier && ts.isStringLiteral(node.moduleSpecifier)) {
            imports.push(node.moduleSpecifier.text);
        }
        if (ts.isCallExpression(node) && node.arguments.length === 1 && ts.isStringLiteral(node.arguments[0]) &&
            (node.expression.kind === ts.SyntaxKind.ImportKeyword ||
                (ts.isIdentifier(node.expression) && node.expression.text === 'require'))) {
            imports.push(node.arguments[0].text);
        }
        ts.forEachChild(node, visit);
    };
    visit(source);
    return imports;
};

const packageName = (specifier: string) => specifier.startsWith('@')
    ? specifier.split('/').slice(0, 2).join('/')
    : specifier.split('/')[0];

describe('when shipping runtime imports', () => {
    it('should declare every external import in both built formats', () => {
        const undeclared = ['esm', 'cjs'].flatMap(format => runtimeFiles(join(packageRoot, 'dist', format))
            .flatMap(file => importsIn(file).filter(specifier => !specifier.startsWith('.') && !specifier.startsWith('node:'))
                .filter(specifier => {
                    const name = packageName(specifier);
                    return name !== manifest.name && !manifest.dependencies?.[name] && !manifest.peerDependencies?.[name];
                }).map(specifier => `${file}: ${specifier}`)));
        undeclared.should.deep.equal([]);
    });

    it('should not bundle another Arc runtime into this package', () => {
        for (const format of ['esm', 'cjs']) {
            existsSync(join(packageRoot, 'dist', format, 'Arc')).should.be.false;
        }
    });
});
