// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { rollup } from '../../../rollup.config.mjs';

import pkg from './package.json' with { type: 'json' };

import path from "path";
import { builtinModules } from 'node:module';

const cjsPath = path.dirname(pkg.main);
const esmPath = path.dirname(pkg.module);
const tsconfigPath = path.join(import.meta.dirname, "tsconfig.json");

const config = rollup(cjsPath, esmPath, tsconfigPath, pkg);

// This Vite plugin runs in Node; its built-ins are provided by the host, not bundled.
config.external.push(...builtinModules.flatMap(name => [name, `node:${name}`]));

export default config;
