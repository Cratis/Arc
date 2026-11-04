// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import ts from 'typescript-for-eslint';

const contents = JSON.parse(fs.readFileSync(0, 'utf8'));
assert.equal(contents.length, 24);
const files = new Map(contents.map((content, index) => {
    assert.doesNotMatch(content, /react|static use/);
    const call = ['execute()', 'perform()', 'subscribe(() => {})'][index % 3];
    return [path.resolve(`.ai-work/neutral-fixture-${index}.ts`), `${content}\nnew Sample().${call};\n`];
}));
const options = {
    strict: true, noEmit: true, skipLibCheck: true, target: ts.ScriptTarget.ES2022,
    module: ts.ModuleKind.ESNext, moduleResolution: ts.ModuleResolutionKind.Bundler,
    experimentalDecorators: true
};
const host = ts.createCompilerHost(options);
const originalRead = host.readFile.bind(host);
const originalExists = host.fileExists.bind(host);
host.readFile = file => files.get(file) ?? originalRead(file);
host.fileExists = file => files.has(file) || originalExists(file);
host.resolveModuleNames = (names, containingFile) => names.map(name => {
    // Deliberately make React and every adapter unavailable, even on a development machine that has them installed.
    if (name === 'react' || name.startsWith('@cratis/arc.react')) throw new Error(`Unexpected adapter dependency: ${name}`);
    return ts.resolveModuleName(name, containingFile, options, host).resolvedModule;
});
const program = ts.createProgram([...files.keys()], options, host);
const diagnostics = ts.getPreEmitDiagnostics(program);
if (diagnostics.length) {
    console.error(ts.formatDiagnosticsWithColorAndContext(diagnostics, {
        getCanonicalFileName: file => file, getCurrentDirectory: () => process.cwd(), getNewLine: () => '\n'
    }));
    process.exit(1);
}
console.log('Compiled 24 framework-neutral command/query/observable proxies and imperative callers with React resolution disabled.');
