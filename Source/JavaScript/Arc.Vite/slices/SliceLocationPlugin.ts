// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Plugin } from 'vite';
import { addSliceLocation } from './addSliceLocation.js';
import { resolveSliceLocation, SliceFolderMapping } from './resolveSliceLocation.js';

/**
 * Options for the {@link SliceLocationPlugin}.
 */
export interface SliceLocationPluginOptions extends SliceFolderMapping {
    /**
     * The files to give a slice location. Defaults to `.tsx` and `.jsx` files.
     */
    include?: RegExp;

    /**
     * The files to leave alone, such as folders holding shared components. Files in `node_modules` are always left alone.
     */
    exclude?: RegExp;

    /**
     * The module to import `withSliceLocation` and `SliceLocation` from. Defaults to `@cratis/arc.react/slices`.
     */
    importFrom?: string;
}

/**
 * Vite plugin that makes components in slice folders provide the slice location of their folder to everything they
 * render, so `useSliceContext()` from `@cratis/arc.react/slices` knows the slice without anything being passed.
 * @remarks
 * The folder a file lives in is mapped to a module, feature and slice the same way the proxy generator maps
 * namespaces, using the roots or the number of segments to skip, filling module, feature and slice from the top.
 * Only exported components are changed: each is wrapped at its declaration so it renders within a slice scope for its
 * folder. Import it from `@cratis/arc.vite/slices`, so importing the other Arc Vite plugins does not load the parser.
 * @param options The {@link SliceLocationPluginOptions}.
 * @returns The Vite plugin.
 */
export const SliceLocationPlugin = (options: SliceLocationPluginOptions = {}): Plugin => {
    const include = options.include ?? /\.(tsx|jsx)$/;
    const exclude = options.exclude;
    let projectRoot = options.projectRoot;

    return {
        name: 'cratis-slice-location',
        enforce: 'pre',

        configResolved(config) {
            projectRoot ??= config.root;
        },

        transform(code, id) {
            const file = id.split('?')[0];
            if (file.startsWith('\0') || /[\\/]node_modules[\\/]/.test(file) || !include.test(file) || exclude?.test(file)) {
                return undefined;
            }

            const location = resolveSliceLocation(file, { ...options, projectRoot });
            return location ? addSliceLocation(code, file, location, options.importFrom) : undefined;
        },
    };
};
