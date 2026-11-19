// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Availability } from '../../Availability.js';

describe('when asking whether a part is available and nothing is known yet', () => {
    const availability = Availability.unknown;

    it('should not report a name as available', () => availability.isAvailable('Fleet.Quality.ScoresReadModel').should.be.false);
    it('should not report a proxy as available', () => availability.isAvailable({ sliceLocation: { module: 'Fleet', feature: 'Quality', slice: 'Scores' } }).should.be.false);
});
