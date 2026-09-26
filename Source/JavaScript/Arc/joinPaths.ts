// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export function joinPaths(...paths: string[]): string {
    let joined = paths[0] ?? '';
    for (const path of paths.slice(1)) {
        const base = joined.endsWith('://') ? joined : joined.replace(/\/+$/, '');
        const segment = path.replace(/^\/+/, '');
        joined = `${base}${base.endsWith('://') ? '' : '/'}${segment}`;
    }
    return joined;
}
