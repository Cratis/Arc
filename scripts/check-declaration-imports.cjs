// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const fs = require('node:fs');
const path = require('node:path');
const { checkDeclarationPackages } = require('./declaration-guard-paths.cjs');

const rootOption = ['--root', '--repo-root'].includes(process.argv[2]) && process.argv.length === 4 && process.argv[3] !== '';
const root = rootOption
    ? path.resolve(process.argv[3]) : path.resolve(__dirname, '..');
const packages = ['Arc', 'Arc.React', 'Arc.React.MVVM', 'Arc.Vite'];

function specifiers(file, source, validateTargets = false) {
    const ts = require('typescript-for-eslint');
    const ast = ts.createSourceFile(file, source, ts.ScriptTarget.Latest, true);
    const found = [];
    function visit(node) {
        let literal;
        if (ts.isImportDeclaration(node) || ts.isExportDeclaration(node)) literal = node.moduleSpecifier;
        if (ts.isImportEqualsDeclaration(node) && ts.isExternalModuleReference(node.moduleReference)) literal = node.moduleReference.expression;
        if (ts.isModuleDeclaration(node) && ts.isStringLiteral(node.name)) literal = node.name;
        if (ts.isImportTypeNode(node) && ts.isLiteralTypeNode(node.argument)) literal = node.argument.literal;
        if (ts.isCallExpression(node) && node.expression.kind === ts.SyntaxKind.ImportKeyword) literal = node.arguments[0];
        if (literal && ts.isStringLiteral(literal) && /^\.\.?\//.test(literal.text)) {
            const line = ast.getLineAndCharacterOfPosition(literal.getStart(ast)).line + 1;
            if (!/\.(js|mjs|cjs|json)$/.test(literal.text)) {
                found.push(`${file}:${line}: extensionless relative import ${literal.text}`);
            } else if (validateTargets && literal.text.endsWith('.js') &&
                !fs.existsSync(path.resolve(root, path.dirname(file), literal.text.replace(/\.js$/, '.d.ts')))) {
                found.push(`${file}:${line}: missing declaration for ${literal.text}`);
            }
        }
        ts.forEachChild(node, visit);
    }
    visit(ast);
    return found;
}

if (process.argv[2] === '--self-test') {
    const violations = specifiers('synthetic.d.ts', "export * from './missing';\ntype T = import('./also-missing').T;\ndeclare module './X' {}\n");
    if (violations.length !== 3 || !violations.includes('synthetic.d.ts:3: extensionless relative import ./X') || specifiers('valid.d.ts', "export * from './valid.js';\n").length) {
        console.error('Declaration import self-test failed');
        process.exit(1);
    }
    console.log('Declaration import self-test passed (3 injected violations detected, including declare module ./X)');
    process.exit(0);
}
if (process.argv.length > 2 && !rootOption) {
    console.error('Usage: node scripts/check-declaration-imports.cjs [--self-test | --root <path> | --repo-root <path>]');
    process.exit(2);
}

let checked;
try {
    checked = checkDeclarationPackages(root, packages);
} catch (error) {
    console.error(error.message);
    process.exit(2);
}
const { checkedPath } = checked;

let count = 0;
const violations = [];
for (const { folder, manifest } of checked.packages) {
    for (const [subpath, branches] of Object.entries(manifest.exports ?? {})) {
        // Retain the dual-format contract for the existing flat import/require condition shape.
        // Other legal export shapes already had every target checked by the shared validator.
        if (!branches || typeof branches !== 'object' || Array.isArray(branches)) continue;
        if (!['import', 'require'].some(branch => Object.hasOwn(branches, branch))) {
            console.error(`${manifest.name} ${subpath}: missing import declaration or runtime condition`);
            process.exit(1);
        }
        const conditions = [branches.import, branches.require].filter(Boolean);
        if (!conditions.every(condition => typeof condition === 'object' && !Array.isArray(condition) &&
            Object.values(condition).every(target => typeof target === 'string'))) continue;
        for (const branch of ['import', 'require']) {
            const condition = branches[branch];
            const expected = branch === 'import' ? 'esm' : 'cjs';
            if (!condition || !condition.types?.startsWith(`./dist/${expected}/`) || !condition.default?.startsWith(`./dist/${expected}/`)) {
                console.error(`${manifest.name} ${subpath}: missing ${branch} declaration or runtime condition`);
                process.exit(1);
            }
            for (const target of [condition.types, condition.default]) {
                const targetPath = path.join(folder, target);
                if (!fs.existsSync(targetPath)) {
                    console.error(`${manifest.name} ${subpath}: missing ${branch} target ${target}`);
                    process.exit(1);
                }
                checkedPath(targetPath);
            }
        }
    }
    for (const format of ['esm', 'cjs']) {
        const dist = path.join(folder, 'dist', format);
        let packageCount = 0;
        function scan(dir) {
            for (const entry of fs.readdirSync(checkedPath(dir), { withFileTypes: true })) {
                if (entry.name === 'node_modules') continue;
                const file = checkedPath(path.join(dir, entry.name));
                if (entry.isDirectory()) scan(file);
                else if (entry.name.endsWith('.d.ts')) {
                    packageCount++;
                    violations.push(...specifiers(path.relative(root, file), fs.readFileSync(file, 'utf8'), format === 'cjs'));
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
}
if (violations.length) {
    console.error(`Found ${violations.length} invalid relative declaration imports:\n${violations.join('\n')}`);
    process.exit(1);
}
console.log(`Checked ${count} ESM/CJS declaration files and export targets across ${packages.length} packages; relative imports resolve`);
