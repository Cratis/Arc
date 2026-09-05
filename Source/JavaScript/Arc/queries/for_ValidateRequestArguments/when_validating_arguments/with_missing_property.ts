// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ValidateRequestArguments } from '../../ValidateRequestArguments';

describe('when validating arguments with a missing required property', () => {
    let result: boolean;

    beforeEach(() => {
        result = ValidateRequestArguments('TestRequest', ['name'], { other: 'value' });
    });

    it('should return false', () => result.should.be.false);
});
