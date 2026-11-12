// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Availability } from '../../Availability.js';

describe('when asking whether a part is available and the server lists its slice', () => {
    const availability = new Availability([{ name: 'Fleet.Quality.Scores.Query', module: 'Fleet', feature: 'Quality', slice: 'Scores' }]);
    class UnavailableProxy {
        static readonly sliceLocation = { module: 'Fleet', feature: 'Quality', slice: 'Scores' };
    }
    class AvailableProxy {
        static readonly sliceLocation = { module: 'Fleet', feature: 'Billing', slice: 'Invoices' };
    }
    class UnlocatedProxy {
        static readonly sliceLocation = { module: '', feature: '', slice: '' };
    }

    it('should report a proxy class of that slice as not available', () => availability.isAvailable(UnavailableProxy).should.be.false);
    it('should report an instance of a proxy of that slice as not available', () => availability.isAvailable(new UnavailableProxy()).should.be.false);
    it('should report a proxy of another slice as available', () => availability.isAvailable(AvailableProxy).should.be.true);
    it('should report a proxy with no slice as available', () => availability.isAvailable(UnlocatedProxy).should.be.true);
});
