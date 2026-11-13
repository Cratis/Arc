// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { field } from '@cratis/fundamentals';
import { UnavailablePart } from './UnavailablePart.js';

/**
 * The part of an unavailable-parts message that names one part. Matches {@link UnavailablePart}.
 */
export class UnavailablePartModel implements UnavailablePart {
    @field(String)
    name = '';

    @field(String)
    module = '';

    @field(String)
    feature = '';

    @field(String)
    slice = '';
}

/**
 * What is not available to the current context, as the server reports it. Everything not listed is available.
 */
export class AvailableParts {
    @field(UnavailablePartModel, true)
    unavailable: UnavailablePartModel[] = [];
}
