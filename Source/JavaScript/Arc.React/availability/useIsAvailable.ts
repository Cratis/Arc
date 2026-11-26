// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useState } from 'react';
import { Availability, AvailabilityPart, PartAvailability, askAvailability, partNameOf } from '@cratis/arc/availability';
import { useAvailability } from './useAvailability.js';

/**
 * Gets whether a part is available, and if not why: `NotAvailable` or `Suspended`.
 *
 * Without `args` it answers for the context alone (the tenant and the user), from what the {@link AvailabilityProvider}
 * reports live. With `args` it asks the server about the data the part is about, for example one vessel, and the server
 * decides with the same gate it uses when the command or query runs. The answer is asked again whenever the
 * availability of the context changes.
 *
 * Until the server has answered, nothing is reported as available.
 * @param part The full name of a command or read model, or a generated proxy.
 * @param args The arguments of the command or query, by property name, for a gate that reads `arguments.name`.
 * @returns The {@link PartAvailability}.
 */
export function useIsAvailable(part: AvailabilityPart, args?: Record<string, unknown>): PartAvailability {
    const availability = useAvailability();
    const name = partNameOf(part);
    const argsJson = args === undefined ? undefined : JSON.stringify(args);
    const key = `${name}\n${argsJson}`;
    const [asked, setAsked] = useState<{ key: string; answer: PartAvailability } | undefined>(undefined);
    const asksServer = argsJson !== undefined && name !== undefined && availability !== Availability.everything;

    useEffect(() => {
        if (!asksServer) {
            return undefined;
        }

        let cancelled = false;
        askAvailability(name!, JSON.parse(argsJson!) as Record<string, unknown>)
            .then(answer => !cancelled && setAsked({ key, answer }))
            .catch(() => !cancelled && setAsked({ key, answer: PartAvailability.unknown }));
        return () => {
            cancelled = true;
        };
    }, [asksServer, name, argsJson, availability]);

    if (argsJson === undefined || name === undefined) {
        return availability.availabilityOf(part);
    }

    if (availability === Availability.everything) {
        return PartAvailability.available;
    }

    return asked?.key === key ? asked.answer : PartAvailability.unknown;
}
