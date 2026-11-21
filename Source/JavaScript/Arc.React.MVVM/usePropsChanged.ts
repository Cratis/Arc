// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useRef } from 'react';
import type { IPropsChanged } from './IPropsChanged.js';

/** Compares shallow snapshots containing only own enumerable props. */
const haveSameProps = (previous: object, next: object): boolean => {
    const keys = Reflect.ownKeys(next);
    if (keys.length !== Reflect.ownKeys(previous).length) return false;
    return keys.every(key => {
        if (!Object.hasOwn(previous, key)) return false;
        return Object.is(Reflect.get(previous, key), Reflect.get(next, key));
    });
};

/** Tracks only committed props, scoped to a single view model instance. */
export function usePropsChanged<TProps extends object>(viewModel: object | null, props: TProps) {
    const previous = useRef<{ viewModel: object; props: TProps } | undefined>(undefined);
    useEffect(() => {
        if (viewModel === null) return;
        const committed = previous.current;
        const snapshot = { ...props };
        previous.current = { viewModel, props: snapshot };
        if (!committed) return;
        if (committed.viewModel !== viewModel) return;
        if (haveSameProps(committed.props, snapshot)) return;
        const handler = viewModel as Partial<IPropsChanged<TProps>>;
        if (typeof handler.propsChanged === 'function') handler.propsChanged(props, committed.props);
    });
}
