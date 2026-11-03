// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ReactNode } from 'react';
import { ApplicationContextContribution } from './ApplicationContextContribution.js';
import { useContributeApplicationContext } from './useContributeApplicationContext.js';

/**
 * Properties for the {@link ApplicationContextContributor} component.
 */
export interface ApplicationContextContributorProps extends ApplicationContextContribution {
    children?: ReactNode;
}

/**
 * Contributes the screen and host values to the application context while mounted.
 * @param props The {@link ApplicationContextContributorProps}.
 */
export const ApplicationContextContributor = (props: ApplicationContextContributorProps) => {
    useContributeApplicationContext({ screen: props.screen, values: props.values });
    return <>{props.children}</>;
};
