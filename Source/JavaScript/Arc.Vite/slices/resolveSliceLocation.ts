// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import path from 'path';

/**
 * Represents the module, feature and slice resolved for a folder.
 */
export interface SliceLocationParts {
    module: string;
    feature: string;
    slice: string;
}

/**
 * Defines how folders map to slice locations; the folder counterpart of the namespace roots and segments to skip the
 * proxy generator uses for namespaces.
 */
export interface SliceFolderMapping {
    /**
     * Folders whose subfolders are modules, features and slices, absolute or relative to the project root. A matching
     * root wins over segment skipping; when several match, the longest one is used.
     */
    roots?: string[];

    /**
     * The number of leading folders, relative to the project root, to skip for files under none of the roots. When not
     * set, files under none of the roots get no slice location.
     */
    segmentsToSkip?: number;

    /**
     * The folder roots and the files relative to it. Defaults to the current working directory.
     */
    projectRoot?: string;
}

const segmentsOf = (relativePath: string) => relativePath.split(/[\\/]/).filter(segment => segment.length > 0 && segment !== '.');

const isWithin = (folder: string, root: string) => {
    const relative = path.relative(root, folder);
    return relative === '' || (!relative.startsWith('..') && !path.isAbsolute(relative));
};

/**
 * Splits the segments that locate a folder into module, feature and slice, the same way the proxy generator splits
 * namespace segments: the first is the module, the last the slice and those between the feature, joined with a period.
 * With fewer than three segments the deepest parts are filled and the rest are empty.
 * @param segments The segments that locate the folder.
 * @returns The slice location parts.
 */
export function toSliceLocationParts(segments: string[]): SliceLocationParts {
    switch (segments.length) {
        case 0: return { module: '', feature: '', slice: '' };
        case 1: return { module: '', feature: '', slice: segments[0] };
        case 2: return { module: '', feature: segments[0], slice: segments[1] };
        default: return {
            module: segments[0],
            feature: segments.slice(1, -1).join('.'),
            slice: segments[segments.length - 1]
        };
    }
}

/**
 * Resolves the slice location of the folder a file lives in.
 * @param file The absolute path of the file.
 * @param mapping The {@link SliceFolderMapping} to resolve with.
 * @returns The slice location parts, or undefined when the file is outside the mapping or its folder is a root.
 */
export function resolveSliceLocation(file: string, mapping: SliceFolderMapping): SliceLocationParts | undefined {
    const projectRoot = path.resolve(mapping.projectRoot ?? process.cwd());
    const folder = path.dirname(path.resolve(projectRoot, file));

    const root = (mapping.roots ?? [])
        .map(candidate => path.resolve(projectRoot, candidate))
        .filter(candidate => isWithin(folder, candidate))
        .sort((first, second) => second.length - first.length)[0];

    let segments: string[];
    if (root !== undefined) {
        segments = segmentsOf(path.relative(root, folder));
    } else if (mapping.segmentsToSkip !== undefined && isWithin(folder, projectRoot)) {
        segments = segmentsOf(path.relative(projectRoot, folder)).slice(mapping.segmentsToSkip);
    } else {
        return undefined;
    }

    return segments.length === 0 ? undefined : toSliceLocationParts(segments);
}
