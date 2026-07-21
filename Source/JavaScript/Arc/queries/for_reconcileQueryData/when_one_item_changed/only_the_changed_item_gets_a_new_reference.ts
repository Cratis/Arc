// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { reconcileQueryData } from '../../reconcileQueryData';

describe('when reconciling a snapshot in which a single item changed', () => {
    const previous = [
        { id: 'a', name: 'First' },
        { id: 'b', name: 'Second' },
        { id: 'c', name: 'Third' }
    ];

    const next = [
        { id: 'a', name: 'First' },
        { id: 'b', name: 'Second changed' },
        { id: 'c', name: 'Third' }
    ];

    const result = reconcileQueryData(previous, next);

    it('should return a new collection', () => result.should.not.equal(previous));
    it('should keep the reference of the first unchanged item', () => result[0].should.equal(previous[0]));
    it('should keep the reference of the last unchanged item', () => result[2].should.equal(previous[2]));
    it('should take the new reference for the changed item', () => result[1].should.equal(next[1]));
    it('should carry the changed value', () => result[1].name.should.equal('Second changed'));
});
