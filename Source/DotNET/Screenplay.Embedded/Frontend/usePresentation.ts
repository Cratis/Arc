// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useState } from 'react';
import type { EventModelPresentation } from '@cratis/event-models';
import { readPresentation, writePresentation } from './presentation';

/** How a view option is changed: from what is shown now to what the person asked for. */
export type PresentationChange = (current: EventModelPresentation) => EventModelPresentation;

/** What the viewer shows, and what it has to admit when a choice could not be remembered. */
export interface PresentationState {
    presentation: EventModelPresentation;
    change: (update: PresentationChange) => void;
    storageFailure?: string;
}

const storageOf = (): Storage | undefined => {
    try {
        return window.localStorage ?? undefined;
    } catch {
        // Accessing localStorage throws outright in browsers where storage is blocked for the site.
        return undefined;
    }
};

/**
 * The view options the board is drawn with, remembered between visits. A change is always shown, and
 * it is also written to storage; when that write fails the viewer says so instead of letting the
 * person believe the choice will still be there after a reload.
 */
export const usePresentation = (): PresentationState => {
    const [presentation, setPresentation] = useState<EventModelPresentation>(() => readPresentation(storageOf()));
    const [storageFailure, setStorageFailure] = useState<string>();

    const change = useCallback((update: PresentationChange) => {
        setPresentation(current => {
            const next = update(current);
            try {
                writePresentation(storageOf(), next);
                setStorageFailure(undefined);
            } catch (error) {
                setStorageFailure(error instanceof Error ? error.message : String(error));
            }
            return next;
        });
    }, []);

    return { presentation, change, storageFailure };
};
