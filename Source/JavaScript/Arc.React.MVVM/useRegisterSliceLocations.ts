// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useContext, useEffect } from 'react';
import { SliceLocation } from '@cratis/arc/reflection';
import { SliceScopeContext } from '@cratis/arc.react/slices';

/**
 * Register slice locations as in use on the nearest slice scope for as long as the component is mounted.
 * @remarks
 * `withViewModel` uses this for the commands and queries resolved for its view model, the counterpart of what the
 * command and query hooks register for themselves. The registrations are kept apart from the creation of the view
 * model, so they survive the effects React runs twice in strict mode.
 * @param locations The slice locations to register.
 */
export function useRegisterSliceLocations(locations: SliceLocation[]): void {
    const scope = useContext(SliceScopeContext);

    useEffect(() => {
        if (!scope) {
            return undefined;
        }
        const unregisters = locations.map(location => scope.register(location));
        return () => unregisters.forEach(unregister => unregister());
    }, [scope, locations]);
}
