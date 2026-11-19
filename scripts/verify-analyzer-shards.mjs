// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// The analyzer build is split into two shards by the project list in Directory.Build.props. Every project outside
// the list is in shard 1, so no project can be left out; what can go wrong is a listed name that no longer matches a
// project, which silently moves nothing and unbalances the shards. This fails on such a name.
import { readFileSync } from 'node:fs';
import { basename } from 'node:path';

const props = readFileSync('Directory.Build.props', 'utf8');
const listed = /<CratisAnalyzerShard2Projects>([^<]*)<\/CratisAnalyzerShard2Projects>/.exec(props);
if (!listed) {
    console.error('Directory.Build.props does not define CratisAnalyzerShard2Projects.');
    process.exit(1);
}

const names = listed[1].split(';').map(name => name.trim()).filter(name => name.length > 0);
const projects = new Set(
    [...readFileSync('Arc.slnx', 'utf8').matchAll(/Path="([^"]+\.csproj)"/g)]
        .map(match => basename(match[1].replaceAll('\\', '/'), '.csproj')));

const unknown = names.filter(name => !projects.has(name));
const duplicated = names.filter((name, index) => names.indexOf(name) !== index);

for (const name of unknown) console.error(`Analyzer shard 2 lists '${name}', which is not a project in Arc.slnx.`);
for (const name of duplicated) console.error(`Analyzer shard 2 lists '${name}' more than once.`);

if (names.length === 0 || unknown.length > 0 || duplicated.length > 0) {
    process.exit(1);
}

console.log(`Analyzer shard 2 lists ${names.length} projects; the other ${projects.size - names.length} are in shard 1.`);
