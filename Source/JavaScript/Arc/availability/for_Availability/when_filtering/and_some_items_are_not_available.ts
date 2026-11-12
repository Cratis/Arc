// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Availability } from '../../Availability.js';

describe('when filtering and some items are not available', () => {
    const availability = new Availability([{ name: 'Fleet.Quality.Scores', module: 'Fleet', feature: 'Quality', slice: 'Scores' }]);
    const items = [
        { title: 'Scores', part: 'Fleet.Quality.Scores' },
        { title: 'Invoices', part: 'Fleet.Billing.Invoices' },
        { title: 'Home', part: undefined as string | undefined }
    ];
    const result = availability.filter(items, _ => _.part);

    it('should drop the item that is not available', () => result.map(_ => _.title).should.deep.equal(['Invoices', 'Home']));
});
