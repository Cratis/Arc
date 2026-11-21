// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useRef } from 'react';
import { deepEqual } from '@cratis/arc';
import type { IPropsChanged } from './IPropsChanged.js';

/** Tracks only committed props, scoped to a single view model instance. */
export function usePropsChanged<TProps extends object>(viewModel: object | null, props: TProps) {
    const previous = useRef<{ viewModel: object; props: TProps } | undefined>(undefined);
    useEffect(() => {
        if (viewModel === null) return;
        const committed = previous.current;
        previous.current = { viewModel, props };
        if (!committed || committed.viewModel !== viewModel || deepEqual(committed.props, props)) return;
        const handler = viewModel as Partial<IPropsChanged<TProps>>;
        if (typeof handler.propsChanged === 'function') handler.propsChanged(props, committed.props);
    }, [viewModel, props]);
}
