// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useMemo, useRef } from 'react';
import { Availability, AvailableParts, ObserveAvailableParts } from '@cratis/arc/availability';
import { useObservableQuery } from '../queries/useObservableQuery.js';
import { AvailabilityContext } from './AvailabilityContext.js';

/**
 * Props for {@link AvailabilityProvider}.
 */
export interface AvailabilityProviderProps {
    /** The application to provide availability to. */
    children: React.ReactNode;
}

/**
 * Provides what is available to the current context to the application, and updates it live.
 *
 * It observes the availability query of the server, which reports what is not available now and again every time that
 * changes, so open sessions update without a reload. Everything the server does not list is available.
 *
 * Until the first decision has arrived nothing is reported as available. If a later update fails, the last decision
 * stays in force: a failure to decide is never taken as a denial.
 * @param props The {@link AvailabilityProviderProps}.
 * @returns The provider element.
 */
export function AvailabilityProvider(props: AvailabilityProviderProps): React.ReactElement {
    const [result] = useObservableQuery<AvailableParts, ObserveAvailableParts>(ObserveAvailableParts);
    const last = useRef<Availability>(Availability.unknown);

    const availability = useMemo(() => {
        if (result.isReady && result.isSuccess && result.isAuthorized && result.data?.unavailable !== undefined) {
            last.current = new Availability(result.data.unavailable);
        }

        return last.current;
    }, [result]);

    return <AvailabilityContext.Provider value={availability}>{props.children}</AvailabilityContext.Provider>;
}
