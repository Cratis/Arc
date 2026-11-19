// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Availability } from '../../Availability.js';

describe('when asking whether a part is available and the server lists it by name', () => {
    const availability = new Availability([{ name: 'Fleet.Quality.ResetScore', module: 'Fleet', feature: 'Quality', slice: 'ResetScore' }]);

    it('should report the listed part as not available', () => availability.isAvailable('Fleet.Quality.ResetScore').should.be.false);
    it('should report another part as available', () => availability.isAvailable('Fleet.Quality.Scores').should.be.true);
});
