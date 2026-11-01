// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useRef } from 'react';
import { ApplicationContextContribution, ApplicationContextContributionHandle } from './ApplicationContextContribution.js';
import { useApplicationContextStore } from './useApplicationContextStore.js';

/**
 * Contribute the screen and host values to the application context for as long as the component is mounted.
 * @remarks
 * The contribution may be a new object on every render; the snapshot only changes when its content does.
 * @param contribution What to contribute.
 */
export function useContributeApplicationContext(contribution: ApplicationContextContribution): void {
    const store = useApplicationContextStore();
    const handle = useRef<ApplicationContextContributionHandle | undefined>(undefined);
    const latest = useRef(contribution);
    latest.current = contribution;

    useEffect(() => {
        handle.current = store?.contribute(latest.current);
        return () => {
            handle.current?.remove();
            handle.current = undefined;
        };
    }, [store]);

    useEffect(() => {
        handle.current?.update(contribution);
    });
}
