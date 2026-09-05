// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ValidateRequestArguments } from '../../ValidateRequestArguments';

describe('when validating arguments with an empty string value', () => {
    let result: boolean;

    beforeEach(() => {
        result = ValidateRequestArguments('TestRequest', ['name'], { name: '' });
    });

    it('should return false', () => result.should.be.false);
});
