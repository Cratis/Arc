// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const fs = require('node:fs');
const path = require('node:path');

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

let realRoot;
try {
    const stat = fs.lstatSync(root);
    if (stat.isSymbolicLink() || !stat.isDirectory()) throw new Error('root must be a real directory, not a symlink');
    realRoot = fs.realpathSync(root);
} catch (error) {
    console.error(`Invalid checked root ${root}: ${error.message}`);
    process.exit(2);
}

function checkedPath(file) {
    try {
        const relative = path.relative(root, file);
        if (relative === '..' || relative.startsWith(`..${path.sep}`) || path.isAbsolute(relative)) throw new Error('path escapes checked root');
        let current = root;
        for (const part of relative.split(path.sep).filter(Boolean)) {
            current = path.join(current, part);
            if (fs.lstatSync(current).isSymbolicLink()) throw new Error(`symlink is not allowed: ${current}`);
        }
        const resolved = path.relative(realRoot, fs.realpathSync(file));
        if (resolved === '..' || resolved.startsWith(`..${path.sep}`) || path.isAbsolute(resolved)) throw new Error('real path escapes checked root');
        return file;
    } catch (error) {
        console.error(`Invalid checked path ${file}: ${error.message}`);
        process.exit(2);
    }
}

function readManifest(file) {
    const checked = checkedPath(file);
    try {
        return JSON.parse(fs.readFileSync(checked, 'utf8'));
    } catch (error) {
        console.error(`Invalid package manifest ${file}: ${error.message}`);
        process.exit(2);
    }
}

let count = 0;
const violations = [];
for (const name of packages) {
    const manifest = readManifest(path.join(root, 'Source/JavaScript', name, 'package.json'));
    for (const [subpath, branches] of Object.entries(manifest.exports)) {
        if (typeof branches === 'string') continue; // package.json and CSS assets
        for (const branch of ['import', 'require']) {
            const condition = branches[branch];
            const expected = branch === 'import' ? 'esm' : 'cjs';
            if (!condition || !condition.types?.startsWith(`./dist/${expected}/`) || !condition.default?.startsWith(`./dist/${expected}/`)) {
                console.error(`${manifest.name} ${subpath}: missing ${branch} declaration or runtime condition`);
                process.exit(1);
            }
            for (const target of [condition.types, condition.default]) {
                const targetPath = path.join(root, 'Source/JavaScript', name, target);
                if (!fs.existsSync(targetPath)) {
                    console.error(`${manifest.name} ${subpath}: missing ${branch} target ${target}`);
                    process.exit(1);
                }
                checkedPath(targetPath);
            }
        }
    }
    for (const format of ['esm', 'cjs']) {
        const dist = path.join(root, 'Source/JavaScript', name, 'dist', format);
        let packageCount = 0;
        function scan(dir) {
            for (const entry of fs.readdirSync(checkedPath(dir), { withFileTypes: true })) {
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
