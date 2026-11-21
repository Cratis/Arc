// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const fs = require('node:fs');
const path = require('node:path');

const distributionModules = { esm: 'module', cjs: 'commonjs' };

function contains(parent, file) {
    const relative = path.relative(parent, file);
    return relative !== '..' && !relative.startsWith(`..${path.sep}`) && !path.isAbsolute(relative);
}

function barePackage(name) {
    const parts = name.split('/');
    return Boolean(name) && !name.startsWith('.') && (!name.startsWith('@') || parts.length >= 2) &&
        parts.every(part => part !== '.' && part !== '..' && /^[\w@*+.-]+$/.test(part));
}

function validateDeclarationReferences(file, dist, source) {
    const ts = require('typescript-for-eslint');
    let ast;
    try {
        ast = ts.createSourceFile(file, source, ts.ScriptTarget.Latest, true);
    } catch (error) {
        throw new Error(`${file}:1: Cannot parse declaration: ${error.message}`);
    }
    function fail(position, message) {
        const line = ast.getLineAndCharacterOfPosition(position).line + 1;
        throw new Error(`${file}:${line}: ${message}`);
    }
    if (ast.parseDiagnostics.length) {
        const diagnostic = ast.parseDiagnostics[0];
        fail(diagnostic.start ?? 0, `Invalid declaration syntax: ${ts.flattenDiagnosticMessageText(diagnostic.messageText, ' ')}`);
    }

    function reference(name, position, kind = 'module') {
        const invalid = () => fail(position, `Invalid declaration reference ${JSON.stringify(name)}: expected a bare package or a path inside package dist`);
        if (!name || name.includes('\\') || name.includes('\0') || path.isAbsolute(name) || path.win32.isAbsolute(name) ||
            /^[a-z][a-z\d+.-]*:/i.test(name)) invalid();
        if (kind === 'types' || kind === 'lib') {
            if (!barePackage(name)) invalid();
        } else if (kind === 'path' || name === '.' || name === '..' || name.startsWith('./') || name.startsWith('../')) {
            if (!contains(dist, path.resolve(path.dirname(fs.realpathSync(file)), name))) invalid();
        } else if (!barePackage(name)) invalid();
    }

    const references = ts.preProcessFile(source, true, true);
    for (const [kind, entries] of [
        ['module', references.importedFiles], ['path', references.referencedFiles],
        ['types', references.typeReferenceDirectives], ['lib', references.libReferenceDirectives]
    ]) {
        for (const entry of entries) reference(entry.fileName, entry.pos, kind);
    }
    // The compiler fields select one attribute per directive. Check every parsed attribute, including combinations.
    const pragmas = ast.pragmas?.get('reference');
    for (const pragma of pragmas ? (Array.isArray(pragmas) ? pragmas : [pragmas]) : []) {
        for (const kind of ['path', 'types', 'lib']) {
            const value = pragma.arguments[kind];
            if (value) reference(value.value, value.pos, kind);
        }
        const noDefaultLib = pragma.arguments['no-default-lib'];
        if (noDefaultLib !== undefined && noDefaultLib !== 'true') fail(pragma.range.pos, 'Invalid no-default-lib directive');
    }

    function literal(node, position) {
        if (!node || (!ts.isStringLiteral(node) && !ts.isNoSubstitutionTemplateLiteral(node))) {
            fail(position, 'Module reference must be a string literal');
        }
        reference(node.text, node.getStart(ast));
    }
    function visit(node) {
        if ((ts.isImportDeclaration(node) || ts.isExportDeclaration(node)) && node.moduleSpecifier) literal(node.moduleSpecifier, node.pos);
        if (ts.isImportEqualsDeclaration(node) && ts.isExternalModuleReference(node.moduleReference)) literal(node.moduleReference.expression, node.pos);
        if (ts.isModuleDeclaration(node) && ts.isStringLiteral(node.name)) literal(node.name, node.pos);
        if (ts.isImportTypeNode(node)) literal(ts.isLiteralTypeNode(node.argument) ? node.argument.literal : undefined, node.pos);
        if (ts.isCallExpression(node) && (node.expression.kind === ts.SyntaxKind.ImportKeyword ||
            (ts.isIdentifier(node.expression) && node.expression.text === 'require'))) literal(node.arguments[0], node.pos);
        ts.forEachChild(node, visit);
    }
    visit(ast);
}

function validateDistributionManifest(file, dist) {
    const format = Object.keys(distributionModules).find(name => path.relative(dist, file) === path.join(name, 'package.json'));
    if (!format) throw new Error(`Nested package.json is not allowed in dist: ${file}`);
    try {
        if (!fs.lstatSync(file).isFile()) throw new Error('manifest must be a regular file');
        const manifest = JSON.parse(fs.readFileSync(file, 'utf8'));
        if (!manifest || Array.isArray(manifest) || Object.keys(manifest).length !== 1 || manifest.type !== distributionModules[format]) {
            throw new Error(`expected exactly ${JSON.stringify({ type: distributionModules[format] })}`);
        }
    } catch (error) {
        throw new Error(`Invalid distribution manifest ${file}: ${error.message}`);
    }
}

function checkDeclarationPackages(root, names) {
    let realRoot;
    try {
        const stat = fs.lstatSync(root);
        if (stat.isSymbolicLink() || !stat.isDirectory()) throw new Error('root must be a real directory, not a symlink');
        realRoot = fs.realpathSync(root);
    } catch (error) {
        throw new Error(`Invalid checked root ${root}: ${error.message}`);
    }

    function checkedPath(file) {
        try {
            if (!contains(root, file)) throw new Error('path escapes checked root');
            let current = root;
            for (const part of path.relative(root, file).split(path.sep).filter(Boolean)) {
                current = path.join(current, part);
                const stat = fs.lstatSync(current);
                if (stat.isSymbolicLink()) throw new Error(`symlink is not allowed: ${current}`);
                if (!stat.isFile() && !stat.isDirectory()) throw new Error(`not a regular file or directory: ${current}`);
            }
            if (!contains(realRoot, fs.realpathSync(file))) throw new Error('real path escapes checked root');
            return file;
        } catch (error) {
            throw new Error(`Invalid checked path ${file}: ${error.message}`);
        }
    }

    function walk(folder, onEntry) {
        for (const entry of fs.readdirSync(checkedPath(folder), { withFileTypes: true })) {
            if (entry.name === 'node_modules') continue;
            const file = checkedPath(path.join(folder, entry.name));
            if (onEntry) onEntry(file, entry.isDirectory());
            if (entry.isDirectory()) walk(file, onEntry);
        }
    }

    function checkPackage(name) {
        const folder = checkedPath(path.join(root, 'Source/JavaScript', name));
        walk(folder);
        const manifestPath = checkedPath(path.join(folder, 'package.json'));
        let manifest;
        try {
            if (!fs.lstatSync(manifestPath).isFile()) throw new Error('manifest must be a regular file');
            manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
            if (!manifest || typeof manifest !== 'object' || Array.isArray(manifest)) throw new Error('manifest must be an object');
        } catch (error) {
            throw new Error(`Invalid package manifest ${manifestPath}: ${error.message}`);
        }
        if (Object.hasOwn(manifest, 'typesVersions')) throw new Error(`typesVersions is not allowed in ${manifestPath}`);
        const dist = checkedPath(path.join(folder, 'dist'));
        if (!fs.lstatSync(dist).isDirectory()) throw new Error(`Invalid declaration output directory: ${dist}`);

        function entrypoint(target) {
            if (typeof target !== 'string' || !target) throw new Error(`Invalid declaration entrypoint in ${manifestPath}`);
            const file = checkedPath(path.resolve(folder, target));
            if (!contains(fs.realpathSync(dist), fs.realpathSync(file)) || !fs.lstatSync(file).isFile()) {
                throw new Error(`Declaration entrypoint must be a regular file under package dist: ${target} in ${manifestPath}`);
            }
        }

        function exportTargets(value) {
            if (value === null) return;
            if (typeof value === 'string') entrypoint(value);
            else if (Array.isArray(value)) value.forEach(exportTargets);
            else if (typeof value === 'object') Object.values(value).forEach(exportTargets);
            else throw new Error(`Invalid exports target in ${manifestPath}`);
        }

        for (const field of ['types', 'typings']) {
            if (Object.hasOwn(manifest, field)) entrypoint(manifest[field]);
        }
        if (Object.hasOwn(manifest, 'main')) {
            if (typeof manifest.main !== 'string' || !/\.(?:js|cjs|mjs|d\.ts)$/.test(manifest.main)) {
                throw new Error(`Invalid main entrypoint in ${manifestPath}: expected .js, .cjs, .mjs or .d.ts under package dist`);
            }
            entrypoint(manifest.main);
        }
        if (Object.hasOwn(manifest, 'exports')) {
            if (manifest.exports && typeof manifest.exports === 'object' && !Array.isArray(manifest.exports)) {
                for (const [subpath, target] of Object.entries(manifest.exports)) {
                    // Only this exact metadata export may point outside dist; its regular file was checked above.
                    if (subpath === './package.json' && target === './package.json') continue;
                    exportTargets(target);
                }
            } else exportTargets(manifest.exports);
        }
        const realDist = fs.realpathSync(dist);
        const declarations = [];
        walk(dist, (file, directory) => {
            if (path.basename(file).toLowerCase() === 'package.json') validateDistributionManifest(file, dist);
            if (directory) return;
            const declaration = /\.d\.(ts|mts|cts)$/.test(file);
            if (/\.(ts|tsx|mts|cts)$/i.test(file) && !declaration) throw new Error(`Only declaration TypeScript files are allowed in dist: ${file}`);
            if (declaration) {
                validateDeclarationReferences(file, realDist, fs.readFileSync(file, 'utf8'));
                declarations.push(file);
            }
        });
        return { folder, manifest, declarations };
    }

    return { checkedPath, packages: names.map(checkPackage) };
}

module.exports = { checkDeclarationPackages, distributionModules };
