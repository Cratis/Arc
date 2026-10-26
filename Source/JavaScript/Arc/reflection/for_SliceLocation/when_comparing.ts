// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '../SliceLocation.js';

describe('when comparing', () => {
    const location = new SliceLocation('Billing', 'Invoices', 'Pay');

    it('should be equal to a location with the same parts', () => location.equals(new SliceLocation('Billing', 'Invoices', 'Pay')).should.be.true);
    it('should not be equal when the module differs', () => location.equals(new SliceLocation('Sales', 'Invoices', 'Pay')).should.be.false);
    it('should not be equal when the feature differs', () => location.equals(new SliceLocation('Billing', 'Orders', 'Pay')).should.be.false);
    it('should not be equal when the slice differs', () => location.equals(new SliceLocation('Billing', 'Invoices', 'List')).should.be.false);
});
