// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '../SliceLocation.js';

class NotDeclaring {
}

describe('when getting it of an instance whose class declares none', () => {
    let result: SliceLocation;

    beforeEach(() => {
        result = SliceLocation.of(new NotDeclaring());
    });

    it('should return the empty slice location', () => result.should.equal(SliceLocation.empty));
    it('should have an empty module', () => result.module.should.equal(''));
    it('should have an empty feature', () => result.feature.should.equal(''));
    it('should have an empty slice', () => result.slice.should.equal(''));
});
