// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { reconcileQueryData } from '../../reconcileQueryData';

describe('when reconciling a snapshot with an item added', () => {
    const previous = [
        { id: 'a', name: 'First' },
        { id: 'b', name: 'Second' }
    ];

    const next = [
        { id: 'a', name: 'First' },
        { id: 'b', name: 'Second' },
        { id: 'c', name: 'Third' }
    ];

    const result = reconcileQueryData(previous, next);

    it('should return a new collection', () => result.should.not.equal(previous));
    it('should have the new length', () => result.length.should.equal(3));
    it('should keep the references of the surviving items', () => {
        result[0].should.equal(previous[0]);
        result[1].should.equal(previous[1]);
    });
    it('should take the new reference for the added item', () => result[2].should.equal(next[2]));
});

describe('when reconciling a snapshot with an item removed', () => {
    const previous = [
        { id: 'a', name: 'First' },
        { id: 'b', name: 'Second' },
        { id: 'c', name: 'Third' }
    ];

    const next = [
        { id: 'a', name: 'First' },
        { id: 'c', name: 'Third' }
    ];

    const result = reconcileQueryData(previous, next);

    it('should have the new length', () => result.length.should.equal(2));
    it('should keep the reference of the item that stayed in place', () => result[0].should.equal(previous[0]));
    it('should keep the reference of the item that shifted position', () => result[1].should.equal(previous[2]));
});
