// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '@cratis/arc/reflection';

/**
 * Gets the parts all locations agree on, from the module down: once a part differs, it and the parts below it are
 * left empty.
 * @param locations The locations to combine.
 * @returns The combined location, or undefined when there are none or they agree on nothing.
 */
export function commonLocationOf(locations: SliceLocation[]): SliceLocation | undefined {
    if (locations.length === 0) {
        return undefined;
    }

    const [first, ...rest] = locations;
    const sameModule = rest.every(location => location.module === first.module);
    const sameFeature = sameModule && rest.every(location => location.feature === first.feature);
    const sameSlice = sameFeature && rest.every(location => location.slice === first.slice);

    const common = new SliceLocation(
        sameModule ? first.module : '',
        sameFeature ? first.feature : '',
        sameSlice ? first.slice : '');

    return common.isEmpty ? undefined : common;
}
