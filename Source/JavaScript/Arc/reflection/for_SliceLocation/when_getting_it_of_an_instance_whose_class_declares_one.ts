// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '../SliceLocation.js';

class Declaring {
    static readonly sliceLocation: SliceLocation = new SliceLocation('Billing', 'Invoices', 'Pay');
}

describe('when getting it of an instance whose class declares one', () => {
    let result: SliceLocation;

    beforeEach(() => {
        result = SliceLocation.of(new Declaring());
    });

    it('should return the declared slice location', () => result.should.equal(Declaring.sliceLocation));
});
