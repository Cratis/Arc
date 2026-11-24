// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { answerFor } from '../askAvailability.js';
import { AvailableParts, UnavailablePartModel } from '../AvailableParts.js';

describe('when answering for a part', () => {
    const listed = Object.assign(new UnavailablePartModel(), { name: 'Fleet.Quality.Scores', reason: 'Suspended' });
    const server = Object.assign(new AvailableParts(), { unavailable: [listed] });

    it('should answer that a part the server lists is not available', () => answerFor('Fleet.Quality.Scores', server).isAvailable.should.be.false);
    it('should answer with the reason the server gave', () => answerFor('Fleet.Quality.Scores', server).isSuspended.should.be.true);
    it('should answer that a part the server does not list is available', () => answerFor('Fleet.Billing.Invoice', server).isAvailable.should.be.true);
    it('should answer that every part is available when the server lists none', () => answerFor('Fleet.Quality.Scores', new AvailableParts()).isAvailable.should.be.true);
});
