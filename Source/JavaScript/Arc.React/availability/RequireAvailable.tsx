// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { AvailabilityPart } from '@cratis/arc/availability';
import { useAvailability } from './useAvailability.js';

/**
 * Props for {@link RequireAvailable}.
 */
export interface RequireAvailableProps {
    /** The full name of a command or read model, or a generated proxy, that must be available. */
    part: AvailabilityPart;

    /** The content to show when the part is available. */
    children: React.ReactNode;

    /** Optional content to show while nothing is known yet. Defaults to nothing. */
    whileLoading?: React.ReactNode;

    /** Optional content to show when the part is not available. Defaults to nothing. */
    unavailable?: React.ReactNode;
}

/**
 * Shows its children only while a part of the application is available, and hides them as soon as it is not.
 * @param props The {@link RequireAvailableProps}.
 * @returns The children, or the unavailable or loading content.
 */
export function RequireAvailable(props: RequireAvailableProps): React.ReactElement {
    const availability = useAvailability();

    if (!availability.isKnown) {
        return <>{props.whileLoading ?? null}</>;
    }

    return availability.isAvailable(props.part) ? <>{props.children}</> : <>{props.unavailable ?? null}</>;
}
