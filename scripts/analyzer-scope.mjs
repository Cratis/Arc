// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Decides which projects the analyzer build analyzes. A pull request analyzes the projects its diff reaches and every
// project that references them (the closure); main, scheduled runs, manual runs and anything this script cannot place
// analyze every project. Analyzers are never turned off: the compiler still builds every project, and the full run on
// main reports anything a pull request did not look at.
//
// Usage: node scripts/analyzer-scope.mjs [--base <sha>] [--head <ref>] [--github-output <file>]
// Without --base the scope is full. The outputs are `mode` (full | affected), `shards` (a JSON array of the analyzer
// shards that have work) and `projects` (';Name;Name;', empty for full).
import { appendFileSync, existsSync, readFileSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { posix } from 'node:path';
import { fileURLToPath } from 'node:url';

// Only these roots hold code the analyzer build analyzes; a path elsewhere cannot change an analyzer result.
const RELEVANT_ROOTS = ['Source/DotNET/', 'Specifications/', 'Samples/'];

// A change here can alter how every project builds or is analyzed, so it always analyzes everything.
const SHARED = [
    /(^|\/)Directory\.[^/]+$/,
    /\.(props|targets|ruleset)$/,
    /(^|\/)(global\.json|nuget\.config|stylecop\.json|\.editorconfig|\.globalconfig|Arc\.slnx)$/i,
    /^\.github\/(workflows|actions)\//,
    /^scripts\/(analyzer-scope|verify-analyzer-shards)/,
];

const IGNORED = [/\.md$/i, /^Documentation\//];

const LINK = /<(?:Compile|EmbeddedResource|Content|None)\s+Include="([^"]+)"/g;
const REFERENCE = /<ProjectReference\s+Include="([^"]+)"/g;

const normalize = path => posix.normalize(path.replaceAll('\\', '/'));

/** Reads a project file into its folder, references and linked paths outside the folder. */
export function describeProject(path, text) {
    const folder = posix.dirname(path);
    const references = [...text.matchAll(REFERENCE)].map(match => normalize(posix.join(folder, match[1])));
    const links = [...text.matchAll(LINK)]
        .map(match => match[1])
        .filter(include => include.replaceAll('\\', '/').startsWith('../'))
        .map(include => normalize(posix.join(folder, include.split(/[*?$]/)[0])))
        .filter(link => !(link + '/').startsWith(folder + '/'));
    return { path, name: posix.basename(path, '.csproj'), folder, references, links };
}

// A link is the literal start of an Include path, so a plain prefix match covers a file, a folder and a wildcard such as 'when_binding_*.cs'.
const owns = (project, file) => file.startsWith(project.folder + '/') || project.links.some(link => file.startsWith(link));

/**
 * Computes the scope for a set of changed files.
 * @param {string[]} changed Repository-relative changed paths.
 * @param {ReturnType<typeof describeProject>[]} projects Every analyzed project.
 * @param {Set<string>} shard2 The names of the projects in analyzer shard 2.
 * @returns {{mode: 'full' | 'affected', names: string[], shards: number[]}} The scope.
 */
export function computeScope(changed, projects, shard2) {
    const full = () => ({ mode: 'full', names: [], shards: [1, 2] });
    const selected = new Set();

    for (const raw of changed) {
        const file = normalize(raw);
        if (SHARED.some(pattern => pattern.test(file))) {
            return full();
        }

        if (IGNORED.some(pattern => pattern.test(file)) || !RELEVANT_ROOTS.some(root => file.startsWith(root))) {
            continue;
        }

        const owners = projects.filter(project => owns(project, file));
        if (owners.length === 0) {
            return full();
        }

        owners.forEach(project => selected.add(project.path));
    }

    // Every project that references a changed project, directly or through others, is analyzed with it.
    const pending = [...selected];
    while (pending.length > 0) {
        const current = pending.pop();
        for (const project of projects) {
            if (!selected.has(project.path) && project.references.includes(current)) {
                selected.add(project.path);
                pending.push(project.path);
            }
        }
    }

    // Projects are matched by name in MSBuild, so two projects that share a name are analyzed together.
    const names = [...new Set(projects.filter(project => selected.has(project.path)).map(project => project.name))].sort();
    const shards = [...new Set(names.map(name => (shard2.has(name) ? 2 : 1)))].sort();
    return { mode: 'affected', names, shards };
}

function git(...args) {
    return execFileSync('git', args, { encoding: 'utf8', maxBuffer: 64 * 1024 * 1024 });
}

function option(name) {
    const index = process.argv.indexOf(name);
    return index >= 0 ? process.argv[index + 1] : undefined;
}

function loadProjects() {
    const paths = [...readFileSync('Arc.slnx', 'utf8').matchAll(/Path="([^"]+\.csproj)"/g)].map(match => normalize(match[1]));
    return paths.filter(path => existsSync(path)).map(path => describeProject(path, readFileSync(path, 'utf8')));
}

function loadShard2() {
    const listed = /<CratisAnalyzerShard2Projects>([^<]*)<\/CratisAnalyzerShard2Projects>/.exec(readFileSync('Directory.Build.props', 'utf8'));
    return new Set((listed?.[1] ?? '').split(';').map(name => name.trim()).filter(name => name.length > 0));
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
    const base = option('--base');
    const head = option('--head') ?? 'HEAD';
    let scope;
    try {
        scope = base
            ? computeScope(git('diff', '--name-only', '--no-renames', base, head).split('\n').filter(line => line.length > 0), loadProjects(), loadShard2())
            : { mode: 'full', names: [], shards: [1, 2] };
    } catch (error) {
        // Never narrow the analysis because the diff could not be read.
        console.error(`Could not compute the analyzer scope, analyzing everything: ${error.message}`);
        scope = { mode: 'full', names: [], shards: [1, 2] };
    }

    const projects = scope.mode === 'full' ? '' : `;${scope.names.join(';')};`;
    const lines = [`mode=${scope.mode}`, `shards=${JSON.stringify(scope.shards)}`, `projects=${projects}`];
    console.log(lines.join('\n'));
    const output = option('--github-output');
    if (output) {
        appendFileSync(output, lines.join('\n') + '\n');
    }
}
