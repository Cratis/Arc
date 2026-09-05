// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Based on: https://github.com/arjendeblok/vite-plugin-emit-metadata
//
// TypeScript 7 ships a native (Go) compiler whose npm package no longer exposes
// the programmatic compiler API (ts.transpileModule / ts.sys / config parsing).
// The single-file transpile that emits `Reflect.metadata("design:type", …)` for
// decorated classes is therefore done with @swc/core, which supports the legacy
// decorator + emitDecoratorMetadata transform Arc's dependency injection relies on.

import path from 'path';
import fs from 'fs';
import { transformSync, type JscTarget } from '@swc/core';

const findContent = (fileContent: string, contentRegEx: RegExp) => contentRegEx.test(fileContent);

interface TsConfigCompilerOptions {
    target?: string;
    emitDecoratorMetadata?: boolean;
}

const knownTargets: JscTarget[] = ['es3', 'es5', 'es2015', 'es2016', 'es2017', 'es2018', 'es2019', 'es2020', 'es2021', 'es2022', 'esnext'];

// Reads the compiler options from a tsconfig.json without depending on the
// TypeScript compiler API. Strips comments and trailing commas so a standard
// JSON.parse can handle the tsconfig relaxations, then reads compilerOptions.
const readCompilerOptions = (tsconfigPath: string): TsConfigCompilerOptions => {
    if (!fs.existsSync(tsconfigPath)) return {};

    try {
        const text = fs.readFileSync(tsconfigPath, 'utf-8');
        const withoutComments = text
            .replace(/\\"|"(?:\\"|[^"])*"|(\/\/.*|\/\*[\s\S]*?\*\/)/g, (match, comment) => (comment ? '' : match))
            .replace(/,(\s*[}\]])/g, '$1');
        const parsed = JSON.parse(withoutComments) as { compilerOptions?: TsConfigCompilerOptions };
        return parsed.compilerOptions ?? {};
    } catch {
        return {};
    }
};

const toSwcTarget = (target?: string): JscTarget => {
    const normalized = (target ?? '').toLowerCase() as JscTarget;
    return knownTargets.includes(normalized) ? normalized : 'es2022';
};

export const EmitMetadataPlugin = ({
    tsconfigPath = path.join(process.cwd(), './tsconfig.json'),
    fileRegEx = /\.ts$/,
    contentRegEx = /((?<![\\(\s]\s*['"])@\w*[\w\d]\s*(?![;])[\\((?=\s)])/
} = {}) => {

    let compilerOptions: TsConfigCompilerOptions | null = null;

    return {
        name: 'transform-file',
        enforce: 'pre' as const,

        transform(src: string, id: string) {
            if (!compilerOptions) {
                compilerOptions = readCompilerOptions(tsconfigPath);
                if (compilerOptions.emitDecoratorMetadata === false) {
                    console.error('vite-plugin-metadata: emitDecoratorMetadata not set', compilerOptions);
                }
            }

            if (!fileRegEx.test(id)) return;

            const hasDecorator = findContent(src, contentRegEx);
            if (!hasDecorator) return;

            const result = transformSync(src, {
                filename: id,
                sourceMaps: 'inline',
                configFile: false,
                swcrc: false,
                jsc: {
                    parser: {
                        syntax: 'typescript',
                        tsx: id.endsWith('.tsx'),
                        decorators: true,
                    },
                    transform: {
                        legacyDecorator: true,
                        decoratorMetadata: true,
                    },
                    target: toSwcTarget(compilerOptions.target),
                    keepClassNames: true,
                },
            });

            return {
                code: result.code,
                map: null,
            };
        },
    };
};
