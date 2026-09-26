// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const fs = require('node:fs');
const path = require('node:path');
const ts = require('typescript-for-eslint');

const root = path.resolve(__dirname, '..');
const packages = ['Arc', 'Arc.React', 'Arc.React.MVVM', 'Arc.Vite'];

function specifiers(file, source) {
    const ast = ts.createSourceFile(file, source, ts.ScriptTarget.Latest, true);
    const found = [];
    function visit(node) {
        let literal;
        if (ts.isImportDeclaration(node) || ts.isExportDeclaration(node)) literal = node.moduleSpecifier;
        if (ts.isImportEqualsDeclaration(node) && ts.isExternalModuleReference(node.moduleReference)) literal = node.moduleReference.expression;
        if (ts.isModuleDeclaration(node) && ts.isStringLiteral(node.name)) literal = node.name;
        if (ts.isImportTypeNode(node) && ts.isLiteralTypeNode(node.argument)) literal = node.argument.literal;
        if (ts.isCallExpression(node) && node.expression.kind === ts.SyntaxKind.ImportKeyword) literal = node.arguments[0];
        if (literal && ts.isStringLiteral(literal) && /^\.\.?\//.test(literal.text) && !/\.(js|mjs|cjs|json)$/.test(literal.text)) {
            const line = ast.getLineAndCharacterOfPosition(literal.getStart(ast)).line + 1;
            found.push(`${file}:${line}: ${literal.text}`);
        }
        ts.forEachChild(node, visit);
    }
    visit(ast);
    return found;
}

if (process.argv[2] === '--self-test') {
    const violations = specifiers('synthetic.d.ts', "export * from './missing';\ntype T = import('./also-missing').T;\ndeclare module './X' {}\n");
    if (violations.length !== 3 || !violations.includes('synthetic.d.ts:3: ./X') || specifiers('valid.d.ts', "export * from './valid.js';\n").length) {
        console.error('Declaration import self-test failed');
        process.exit(1);
    }
    console.log('Declaration import self-test passed (3 injected violations detected, including declare module ./X)');
    process.exit(0);
}
if (process.argv.length > 2) {
    console.error('Usage: node scripts/check-declaration-imports.cjs [--self-test]');
    process.exit(2);
}

let count = 0;
const violations = [];
for (const name of packages) {
    const dist = path.join(root, 'Source/JavaScript', name, 'dist/esm');
    let packageCount = 0;
    function scan(dir) {
        for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
            const file = path.join(dir, entry.name);
            if (entry.isDirectory()) scan(file);
            else if (entry.name.endsWith('.d.ts')) {
                packageCount++;
                violations.push(...specifiers(path.relative(root, file), fs.readFileSync(file, 'utf8')));
            }
        }
    }
    if (fs.existsSync(dist)) scan(dist);
    if (!packageCount) {
        console.error(`Missing or empty declaration output: ${dist}`);
        process.exit(2);
    }
    count += packageCount;
}
if (violations.length) {
    console.error(`Found ${violations.length} extensionless relative declaration imports:\n${violations.join('\n')}`);
    process.exit(1);
}
console.log(`Checked ${count} declaration files across ${packages.length} packages; no extensionless relative imports`);
