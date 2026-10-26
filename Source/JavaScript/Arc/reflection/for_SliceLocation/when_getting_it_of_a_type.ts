// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '../SliceLocation.js';

abstract class Declaring {
    static readonly sliceLocation: SliceLocation = new SliceLocation('Billing', 'Invoices', 'Pay');
}

class NotDeclaring {
}

class DeclaringSomethingElse {
    static readonly sliceLocation = 'Billing/Invoices/Pay';
}

describe('when getting it of a type', () => {
    let declared: SliceLocation;
    let undeclared: SliceLocation;
    let notASliceLocation: SliceLocation;

    beforeEach(() => {
        declared = SliceLocation.ofType(Declaring);
        undeclared = SliceLocation.ofType(NotDeclaring);
        notASliceLocation = SliceLocation.ofType(DeclaringSomethingElse);
    });

    it('should return the declared slice location', () => declared.should.equal(Declaring.sliceLocation));
    it('should return the empty slice location when the type declares none', () => undeclared.should.equal(SliceLocation.empty));
    it('should return the empty slice location when the static member is not a slice location', () => notASliceLocation.should.equal(SliceLocation.empty));
});
