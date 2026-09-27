// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

function trimTrailingSlashes(path: string): string {
    let end = path.length;
    while (end > 0 && path[end - 1] === '/') {
        end--;
    }
    return path.slice(0, end);
}

function trimLeadingSlashes(path: string): string {
    let start = 0;
    while (start < path.length && path[start] === '/') {
        start++;
    }
    return path.slice(start);
}

export function joinPaths(...paths: string[]): string {
    let joined = paths[0] ?? '';
    for (const path of paths.slice(1)) {
        const base = joined.endsWith('://') ? joined : trimTrailingSlashes(joined);
        const segment = trimLeadingSlashes(path);
        joined = `${base}${base.endsWith('://') ? '' : '/'}${segment}`;
    }
    return joined;
}
