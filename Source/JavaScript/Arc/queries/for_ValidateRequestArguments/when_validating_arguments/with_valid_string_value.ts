// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ValidateRequestArguments } from '../../ValidateRequestArguments';

describe('when validating arguments with a valid string value', () => {
    let result: boolean;

    beforeEach(() => {
        result = ValidateRequestArguments('TestRequest', ['name'], { name: 'some-value' });
    });

    it('should return true', () => result.should.be.true);
});
