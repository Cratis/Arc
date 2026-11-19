// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObservableQueryFor } from '../queries/ObservableQueryFor.js';
import { ParameterDescriptor } from '../reflection/ParameterDescriptor.js';
import { AvailableParts } from './AvailableParts.js';

/**
 * Observes what is not available to the current context. It reports the current decision first and again every time
 * it changes, so an open session updates without a reload.
 */
export class ObserveAvailableParts extends ObservableQueryFor<AvailableParts> {
    readonly route = '/.cratis/availability';
    readonly defaultValue = new AvailableParts();
    readonly parameterDescriptors: ParameterDescriptor[] = [];

    get requiredRequestParameters(): string[] {
        return [];
    }

    constructor() {
        super(AvailableParts, false);
    }
}
