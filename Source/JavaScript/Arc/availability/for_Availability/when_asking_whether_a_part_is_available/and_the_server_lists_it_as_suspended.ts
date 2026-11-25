// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Availability } from '../../Availability.js';

describe('when asking whether a part is available and the server lists it as suspended', () => {
    const availability = new Availability([
        { name: 'Fleet.Quality.Scores', module: 'Fleet', feature: 'Quality', slice: 'Scores', reason: 'Suspended' },
        { name: 'Fleet.Billing.Invoice', module: 'Fleet', feature: 'Billing', slice: 'Invoices', reason: 'NotAvailable' },
        { name: 'Fleet.Billing.Legacy', module: 'Fleet', feature: 'Billing', slice: 'Legacy' }
    ]);

    it('should tell a suspended part by its reason', () => availability.availabilityOf('Fleet.Quality.Scores').isSuspended.should.be.true);
    it('should not take a suspended part for one that is not available', () => availability.availabilityOf('Fleet.Quality.Scores').isNotAvailable.should.be.false);
    it('should keep a suspended part unavailable', () => availability.isAvailable('Fleet.Quality.Scores').should.be.false);
    it('should tell a part that is not available by its reason', () => availability.availabilityOf('Fleet.Billing.Invoice').isNotAvailable.should.be.true);
    it('should take a missing reason for not available', () => availability.availabilityOf('Fleet.Billing.Legacy').isNotAvailable.should.be.true);
    it('should give no reason for a part that is available', () => (availability.availabilityOf('Fleet.Other').reason === undefined).should.be.true);
    it('should report nothing as available when nothing is known', () => Availability.unknown.availabilityOf('Fleet.Other').isKnown.should.be.false);
});
