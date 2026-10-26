// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '@cratis/arc/reflection';
import { withSliceLocation } from '../withSliceLocation.js';

describe('when wrapping something that is not a component', () => {
    const store = { count: 0 };
    const columns = [{ field: 'amount' }];

    it('should return an object as it is', () => withSliceLocation(store, SliceLocation.empty).should.equal(store));
    it('should return an array as it is', () => withSliceLocation(columns, SliceLocation.empty).should.equal(columns));
});
