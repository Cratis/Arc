// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext } from 'react';
import { Availability } from '@cratis/arc/availability';

/**
 * The context that holds what is available to the current context.
 *
 * Without an {@link AvailabilityProvider} the host is not using availability and everything is available.
 */
export const AvailabilityContext = createContext<Availability>(Availability.everything);
