// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ApplicationScreen } from './ApplicationScreen.js';

/**
 * What a host or router contributes to the application context.
 */
export interface ApplicationContextContribution {
    /**
     * The screen. Parts that are given override what earlier contributors gave.
     */
    screen?: Partial<ApplicationScreen>;

    /**
     * Arbitrary host values, merged by key into {@link ApplicationContextSnapshot.contributions}.
     */
    values?: Record<string, unknown>;
}

/**
 * A live contribution that can be updated and removed.
 */
export interface ApplicationContextContributionHandle {
    /**
     * Replaces what the contribution provides.
     * @param contribution The new contribution.
     */
    update(contribution: ApplicationContextContribution): void;

    /**
     * Removes the contribution.
     */
    remove(): void;
}
