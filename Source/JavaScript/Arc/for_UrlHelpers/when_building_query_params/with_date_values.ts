// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it, beforeEach } from 'vitest';
import { UrlHelpers } from '../../UrlHelpers';

describe('when building query params with date values', () => {
    const startTime = new Date(Date.UTC(2026, 5, 3, 13, 57, 6));
    const endTime = new Date(Date.UTC(2026, 5, 3, 14, 0, 32));
    let result: URLSearchParams;

    beforeEach(() => {
        result = UrlHelpers.buildQueryParams({ startTime, endTime });
    });

    it('should serialize startTime as ISO 8601', () => result.get('startTime')!.should.equal('2026-06-03T13:57:06.000Z'));
    it('should serialize endTime as ISO 8601', () => result.get('endTime')!.should.equal('2026-06-03T14:00:32.000Z'));
});
