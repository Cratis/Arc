// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { RequireAvailable } from '../RequireAvailable.js';
import { an_availability_provider } from './given/an_availability_provider.js';

describe('when the server reports what is not available', () => {
    const context = new an_availability_provider();
    const scores = { name: 'Fleet.Quality.Scores', module: 'Fleet', feature: 'Quality', slice: 'Scores' };

    beforeEach(async () => {
        context.establish();
        context.show(React.createElement('div', null,
            React.createElement(RequireAvailable, { part: 'Fleet.Quality.Scores', whileLoading: 'loading', unavailable: 'gone', children: 'scores' }),
            React.createElement(RequireAvailable, { part: 'Fleet.Billing.Invoices', whileLoading: 'loading', unavailable: 'gone', children: 'invoices' })));
    });

    afterEach(() => {
        context.rendered.unmount();
        context.cleanup();
    });

    it('should show nothing as available before the first decision', () => context.rendered.container.textContent!.should.equal('loadingloading'));

    it('should hide the part that is not available and show the rest', async () => {
        await context.pushUnavailable(scores);
        context.rendered.container.textContent!.should.equal('goneinvoices');
    });

    it('should update live, without a reload, when the part becomes available again', async () => {
        await context.pushUnavailable(scores);
        await context.pushUnavailable();
        context.rendered.container.textContent!.should.equal('scoresinvoices');
    });

    it('should update live when another part stops being available', async () => {
        await context.pushUnavailable();
        await context.pushUnavailable({ name: 'Fleet.Billing.Invoices', module: 'Fleet', feature: 'Billing', slice: 'Invoices' });
        context.rendered.container.textContent!.should.equal('scoresgone');
    });

    it('should keep the last decision when a later update fails', async () => {
        await context.pushUnavailable(scores);
        await context.pushFailure();
        context.rendered.container.textContent!.should.equal('goneinvoices');
    });

    it('should not open anything when the very first update fails', async () => {
        await context.pushFailure();
        context.rendered.container.textContent!.should.equal('loadingloading');
    });
});
