// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { reconcileQueryData } from '../../reconcileQueryData';

describe('when reconciling a single object that did not change', () => {
    const previous = { id: 'a', name: 'First' };
    const next = { id: 'a', name: 'First' };

    const result = reconcileQueryData(previous, next);

    it('should return the previous object', () => result.should.equal(previous));
});

describe('when reconciling a single object that changed', () => {
    const previous = { id: 'a', name: 'First' };
    const next = { id: 'a', name: 'Changed' };

    const result = reconcileQueryData(previous, next);

    it('should return the new object', () => result.should.equal(next));
});

describe('when reconciling items that carry no identity', () => {
    const previous = [{ name: 'First' }, { name: 'Second' }];
    const next = [{ name: 'First' }, { name: 'Second' }];

    const result = reconcileQueryData(previous, next);

    it('should fall back to positional matching and preserve the collection', () => result.should.equal(previous));
});
