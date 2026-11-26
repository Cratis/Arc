// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObservableQueryFor } from '../queries/ObservableQueryFor.js';
import { ParameterDescriptor } from '../reflection/ParameterDescriptor.js';
import { AvailableParts } from './AvailableParts.js';

/**
 * The parameters of {@link ObserveAvailableParts}.
 */
export interface ObserveAvailablePartsParameters {
    /** The full name of one part to ask about. Every part is decided when it is not given. */
    part?: string;

    /** The arguments of that part as a JSON object, for a gate that reads `arguments.name`. */
    arguments?: string;
}

/**
 * Observes what is not available to the current context. It reports the current decision first and again every time
 * it changes, so an open session updates without a reload.
 *
 * With `part` (and `arguments`) it answers for that one part and the data it is about: the part is listed when it is
 * not available for those arguments, and the answer is empty when it is available.
 */
export class ObserveAvailableParts extends ObservableQueryFor<AvailableParts, ObserveAvailablePartsParameters> {
    readonly route = '/.cratis/availability';
    readonly defaultValue = new AvailableParts();
    readonly parameterDescriptors: ParameterDescriptor[] = [
        new ParameterDescriptor('arguments', String, false),
        new ParameterDescriptor('part', String, false),
    ];

    part?: string;
    arguments?: string;

    get requiredRequestParameters(): string[] {
        return [];
    }

    constructor() {
        super(AvailableParts, false);
    }
}
