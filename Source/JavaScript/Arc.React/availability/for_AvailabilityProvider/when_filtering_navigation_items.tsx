// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { an_availability_provider } from './given/an_availability_provider.js';
import { useAvailableItems } from '../useAvailableItems.js';

const items = [
    { title: 'Scores', part: 'Fleet.Quality.Scores' },
    { title: 'Invoices', part: 'Fleet.Billing.Invoices' },
    { title: 'Home', part: undefined as string | undefined }
];
const partOf = (item: { part: string | undefined }) => item.part;

const Navigation = () => {
    const available = useAvailableItems(items, partOf);
    return React.createElement('nav', null, available.map(_ => _.title).join(','));
};

describe('when filtering navigation items', () => {
    const context = new an_availability_provider();

    beforeEach(() => {
        context.establish();
        context.show(React.createElement(Navigation));
    });

    afterEach(() => {
        context.rendered.unmount();
        context.cleanup();
    });

    it('should offer nothing before the first decision', () => context.rendered.container.textContent!.should.equal('Home'));

    it('should drop the item that is not available', async () => {
        await context.pushUnavailable({ name: 'Fleet.Quality.Scores', module: 'Fleet', feature: 'Quality', slice: 'Scores' });
        context.rendered.container.textContent!.should.equal('Invoices,Home');
    });

    it('should offer the item again when it becomes available, without a reload', async () => {
        await context.pushUnavailable({ name: 'Fleet.Quality.Scores', module: 'Fleet', feature: 'Quality', slice: 'Scores' });
        await context.pushUnavailable();
        context.rendered.container.textContent!.should.equal('Scores,Invoices,Home');
    });
});
