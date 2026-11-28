// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// cyrb53 mixes both halves before reducing the result to a safe 53-bit integer.
function cyrb53(text: string): number {
    let low = 0xdeadbeef;
    let high = 0x41c6ce57;
    for (let index = 0; index < text.length; index++) {
        const code = text.charCodeAt(index);
        low = Math.imul(low ^ code, 2654435761);
        high = Math.imul(high ^ code, 1597334677);
    }
    low = Math.imul(low ^ (low >>> 16), 2246822507) ^ Math.imul(high ^ (high >>> 13), 3266489909);
    high = Math.imul(high ^ (high >>> 16), 2246822507) ^ Math.imul(low ^ (low >>> 13), 3266489909);
    return 4294967296 * (high & 2097151) + (low >>> 0);
}

export function fingerprintSuspenseQueryHeaders(headers?: Headers): string {
    // Headers iterates lowercased names in sorted order, combining repeated values as fetch does.
    const pairs = Array.from(headers ?? []);
    // Reserve zero for requests with no custom headers.
    return pairs.length === 0 ? '0' : (cyrb53(JSON.stringify(pairs)) + 1).toString(36);
}
