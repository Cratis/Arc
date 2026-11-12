// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Availability } from '../Availability.js';

describe('when everything is available', () => {
    it('should report any part as available', () => Availability.everything.isAvailable('Fleet.Quality.Scores').should.be.true);
});
