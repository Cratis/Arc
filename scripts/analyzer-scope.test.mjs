// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { test } from 'node:test';
import { computeScope, describeProject } from './analyzer-scope.mjs';

const project = (path, text = '') => describeProject(path, text);
const reference = target => `<ProjectReference Include="${target}" />`;

const core = project('Source/DotNET/Core/Core.csproj');
const mid = project('Source/DotNET/Mid/Mid.csproj', reference('../Core/Core.csproj'));
const midSpecs = project('Source/DotNET/Mid.Specs/Mid.Specs.csproj', reference('..\\Mid\\Mid.csproj'));
const leaf = project('Source/DotNET/Leaf/Leaf.csproj');
const shared = project('Source/DotNET/Linker/Linker.csproj', '<Compile Include="../Shared/Convention.cs" Link="Convention.cs" />\n<Compile Include="../Core.Specs/when_binding_*.cs" />');
const projects = [core, mid, midSpecs, leaf, shared];
const shard2 = new Set(['Mid.Specs']);

const scope = (...changed) => computeScope(changed, projects, shard2);

test('a changed project brings in every project that references it, directly or not', () => {
    const result = scope('Source/DotNET/Core/Thing.cs');
    assert.equal(result.mode, 'affected');
    assert.deepEqual(result.names, ['Core', 'Mid', 'Mid.Specs']);
    assert.deepEqual(result.shards, [1, 2]);
});

test('a leaf project analyzes only itself, and only its shard', () => {
    const result = scope('Source/DotNET/Leaf/Leaf.cs');
    assert.deepEqual(result.names, ['Leaf']);
    assert.deepEqual(result.shards, [1]);
});

test('a spec project analyzes only the specs and shard 2', () => {
    const result = scope('Source/DotNET/Mid.Specs/when_x.cs');
    assert.deepEqual(result.names, ['Mid.Specs']);
    assert.deepEqual(result.shards, [2]);
});

test('a file linked into a project selects that project', () => {
    assert.deepEqual(scope('Source/DotNET/Shared/Convention.cs').names, ['Linker']);
    assert.deepEqual(scope('Source/DotNET/Core.Specs/when_binding_a.cs').names, ['Linker']);
});

test('build and infrastructure files analyze everything', () => {
    for (const path of ['Directory.Build.props', 'Source/DotNET/Directory.Build.targets', 'Directory.Packages.props', 'global.json',
        '.editorconfig', 'Source/DotNET/Core/.editorconfig', '.globalconfig', 'stylecop.json', 'Arc.slnx', '.github/workflows/dotnet-build.yml',
        'scripts/analyzer-scope.mjs', 'Source/DotNET/Core/Core.csproj.props']) {
        assert.equal(scope(path).mode, 'full', path);
    }
});

test('a path inside the analyzed roots that no project owns analyzes everything', () => {
    assert.equal(scope('Source/DotNET/Unowned/file.cs').mode, 'full');
});

test('documentation and files outside the analyzed roots analyze nothing', () => {
    const result = scope('README.md', 'Documentation/a.cs', 'Source/JavaScript/x.ts');
    assert.equal(result.mode, 'affected');
    assert.deepEqual(result.names, []);
    assert.deepEqual(result.shards, []);
});

test('one shared file among project files still analyzes everything', () => {
    assert.equal(scope('Source/DotNET/Leaf/Leaf.cs', 'Directory.Build.props').mode, 'full');
});
