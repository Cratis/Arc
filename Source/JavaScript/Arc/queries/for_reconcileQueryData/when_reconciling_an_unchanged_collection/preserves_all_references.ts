// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { reconcileQueryData } from '../../reconcileQueryData';

describe('when reconciling a re-delivered snapshot that did not change', () => {
    const previous = [
        { id: 'a', name: 'First' },
        { id: 'b', name: 'Second' }
    ];

    // A fresh deserialization of the same payload — equal in value, all-new references.
    const next = [
        { id: 'a', name: 'First' },
        { id: 'b', name: 'Second' }
    ];

    const result = reconcileQueryData(previous, next);

    it('should return the previous collection itself', () => result.should.equal(previous));
    it('should not adopt any of the new references', () => result.forEach((item, index) => item.should.equal(previous[index])));
});
