// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '../SliceLocation.js';

class Declaring {
    static readonly sliceLocation: SliceLocation = new SliceLocation('Billing', 'Invoices', 'Pay');
}

class NotDeclaring {
}

describe('when reading it from a type', () => {
    it('should return the location the type declares', () => SliceLocation.ofType(Declaring).should.equal(Declaring.sliceLocation));
    it('should return the empty location when the type declares none', () => SliceLocation.ofType(NotDeclaring).should.equal(SliceLocation.empty));
});
