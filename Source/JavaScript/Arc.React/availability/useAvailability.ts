// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useContext } from 'react';
import { Availability } from '@cratis/arc/availability';
import { AvailabilityContext } from './AvailabilityContext.js';

/**
 * Gets what is available to the current context. The component re-renders when it changes.
 * @returns The current {@link Availability}.
 */
export function useAvailability(): Availability {
    return useContext(AvailabilityContext);
}
