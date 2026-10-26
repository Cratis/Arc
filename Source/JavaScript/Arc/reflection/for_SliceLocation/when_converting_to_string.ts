// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '../SliceLocation.js';

describe('when converting to string', () => {
    it('should join all parts with a forward slash', () => new SliceLocation('Billing', 'Invoices', 'Pay').toString().should.equal('Billing/Invoices/Pay'));
    it('should leave out empty parts', () => new SliceLocation('', 'Invoices', 'Pay').toString().should.equal('Invoices/Pay'));
});
