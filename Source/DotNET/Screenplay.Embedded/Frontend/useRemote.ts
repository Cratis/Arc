// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useState } from 'react';

/** The state of a single remote load. */
export type Remote<T> =
    | { status: 'loading' }
    | { status: 'loaded'; value: T }
    | { status: 'error'; error: Error };

const asError = (reason: unknown) => reason instanceof Error ? reason : new Error(String(reason));

/**
 * Loads a value for the given keys. A new set of keys aborts the load in flight, so a selection
 * that changes while a request is on the wire can never be overwritten by the answer to the old one.
 */
export const useRemote = <T>(load: (signal: AbortSignal) => Promise<T>, keys: readonly unknown[]): Remote<T> => {
    const [state, setState] = useState<Remote<T>>({ status: 'loading' });

    useEffect(() => {
        const controller = new AbortController();
        setState({ status: 'loading' });
        load(controller.signal).then(
            value => {
                if (!controller.signal.aborted) setState({ status: 'loaded', value });
            },
            reason => {
                if (!controller.signal.aborted) setState({ status: 'error', error: asError(reason) });
            });
        return () => controller.abort();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, keys);

    return state;
};
