// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo } from 'react';
import { AvailabilityPart } from '@cratis/arc/availability';
import { useAvailability } from './useAvailability.js';

/**
 * Keeps the navigation items, menu entries or screens whose part is available, and updates when that changes.
 * @param items The items.
 * @param partOf Gets the part an item belongs to: the full name of a command or read model, or a generated proxy.
 * An item without a part is always kept.
 * @returns The items that are available.
 */
export function useAvailableItems<TItem>(items: readonly TItem[], partOf: (item: TItem) => AvailabilityPart | undefined): TItem[] {
    const availability = useAvailability();
    return useMemo(() => availability.filter(items, partOf), [availability, items, partOf]);
}
