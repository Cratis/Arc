// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ReactNode, useContext, useSyncExternalStore } from 'react';
import { SliceLocation } from '@cratis/arc/reflection';
import { ArcContext } from '../ArcContext.js';
import { SliceScope } from '../slices/SliceScope.js';

/**
 * Gets the data attributes that mark an element with a slice location.
 * @param location The location to mark with.
 * @returns The `data-cratis-module`, `data-cratis-feature` and `data-cratis-slice` attributes.
 */
export function sliceMarkerAttributes(location: SliceLocation): Record<string, string> {
    return {
        'data-cratis-module': location.module,
        'data-cratis-feature': location.feature,
        'data-cratis-slice': location.slice
    };
}

const MarkedChildren = (props: { scope: SliceScope, children?: ReactNode }) => {
    const location = useSyncExternalStore(props.scope.subscribe, props.scope.getSnapshot, props.scope.getSnapshot);
    return <div style={{ display: 'contents' }} {...sliceMarkerAttributes(location)}>{props.children}</div>;
};

/**
 * Wraps children in a `display: contents` element carrying the slice location of the scope, when `sliceMarkers` is
 * enabled on `<Arc>`. When disabled, renders the children as they are.
 * @param props The scope to resolve the location of and the children.
 */
export const SliceMarker = (props: { scope: SliceScope, children?: ReactNode }) => {
    const enabled = useContext(ArcContext).sliceMarkers ?? false;
    return enabled ? <MarkedChildren scope={props.scope}>{props.children}</MarkedChildren> : <>{props.children}</>;
};
