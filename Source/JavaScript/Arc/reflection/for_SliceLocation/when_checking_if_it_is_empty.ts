// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '../SliceLocation.js';

describe('when checking if it is empty', () => {
    it('should be empty when every part is empty', () => SliceLocation.empty.isEmpty.should.be.true);
    it('should not be empty when only the slice is set', () => new SliceLocation('', '', 'Pay').isEmpty.should.be.false);
    it('should not be empty when only the module is set', () => new SliceLocation('Billing', '', '').isEmpty.should.be.false);
});
