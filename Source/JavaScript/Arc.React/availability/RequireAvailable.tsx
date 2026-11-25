// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { AvailabilityPart } from '@cratis/arc/availability';
import { useIsAvailable } from './useIsAvailable.js';

/**
 * Props for {@link RequireAvailable}.
 */
export interface RequireAvailableProps {
    /** The full name of a command or read model, or a generated proxy, that must be available. */
    part: AvailabilityPart;

    /**
     * The arguments of the command or query the content is about, by property name, for a gate that reads
     * `arguments.name`. The server decides with the same gate it uses when the command or query runs.
     */
    args?: Record<string, unknown>;

    /** The content to show when the part is available. */
    children: React.ReactNode;

    /** Optional content to show while nothing is known yet. Defaults to nothing. */
    whileLoading?: React.ReactNode;

    /** Optional content to show when the part is not available. Defaults to nothing. */
    unavailable?: React.ReactNode;

    /**
     * Optional content to show when an operator has suspended the part, which comes back when the suspension is
     * lifted. Defaults to the `unavailable` content.
     */
    suspended?: React.ReactNode;
}

/**
 * Shows its children only while a part of the application is available, and hides them as soon as it is not.
 * @param props The {@link RequireAvailableProps}.
 * @returns The children, or the unavailable, suspended or loading content.
 */
export function RequireAvailable(props: RequireAvailableProps): React.ReactElement {
    const availability = useIsAvailable(props.part, props.args);

    if (!availability.isKnown) {
        return <>{props.whileLoading ?? null}</>;
    }

    if (availability.isAvailable) {
        return <>{props.children}</>;
    }

    return <>{(availability.isSuspended ? props.suspended ?? props.unavailable : props.unavailable) ?? null}</>;
}
