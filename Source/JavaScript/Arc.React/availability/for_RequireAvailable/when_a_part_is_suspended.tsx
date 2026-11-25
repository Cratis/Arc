// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { Availability } from '@cratis/arc/availability';
import { AvailabilityContext } from '../AvailabilityContext.js';
import { RequireAvailable } from '../RequireAvailable.js';

describe('when a part is suspended', () => {
    const availability = new Availability([
        { name: 'Fleet.Quality.Scores', module: 'Fleet', feature: 'Quality', slice: 'Scores', reason: 'Suspended' },
        { name: 'Fleet.Billing.Invoice', module: 'Fleet', feature: 'Billing', slice: 'Invoices', reason: 'NotAvailable' }
    ]);

    const show = (part: string, props: { unavailable?: string; suspended?: string }) => {
        const rendered = render(
            <AvailabilityContext.Provider value={availability}>
                <RequireAvailable part={part} unavailable={props.unavailable} suspended={props.suspended}>content</RequireAvailable>
            </AvailabilityContext.Provider>);
        const text = rendered.container.textContent;
        rendered.unmount();
        return text;
    };

    it('should show the suspended content for a suspended part', () => show('Fleet.Quality.Scores', { unavailable: 'gone', suspended: 'back soon' })!.should.equal('back soon'));
    it('should show the unavailable content for a suspended part that has no suspended content', () => show('Fleet.Quality.Scores', { unavailable: 'gone' })!.should.equal('gone'));
    it('should show the unavailable content, not the suspended content, for a part that is not available', () => show('Fleet.Billing.Invoice', { unavailable: 'gone', suspended: 'back soon' })!.should.equal('gone'));
    it('should show the content of a part that is listed by neither', () => show('Fleet.Other', { unavailable: 'gone', suspended: 'back soon' })!.should.equal('content'));
});
