// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const fs = require('node:fs');
const path = require('node:path');

function contains(parent, file) {
    const relative = path.relative(parent, file);
    return relative !== '..' && !relative.startsWith(`..${path.sep}`) && !path.isAbsolute(relative);
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

    function walk(folder) {
        for (const entry of fs.readdirSync(checkedPath(folder), { withFileTypes: true })) {
            if (entry.name === 'node_modules') continue;
            const file = checkedPath(path.join(folder, entry.name));
            if (entry.isDirectory()) walk(file);
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
        if (typeof manifest.main === 'string' && /\.d\.(ts|mts|cts)$/.test(manifest.main)) entrypoint(manifest.main);
        if (Object.hasOwn(manifest, 'exports')) {
            if (manifest.exports && typeof manifest.exports === 'object' && !Array.isArray(manifest.exports)) {
                for (const [subpath, target] of Object.entries(manifest.exports)) {
                    // Only this exact metadata export may point outside dist; its regular file was checked above.
                    if (subpath === './package.json' && target === './package.json') continue;
                    exportTargets(target);
                }
            } else exportTargets(manifest.exports);
        }
        return { folder, manifest };
    }

    return { checkedPath, packages: names.map(checkPackage) };
}

module.exports = { checkDeclarationPackages };
