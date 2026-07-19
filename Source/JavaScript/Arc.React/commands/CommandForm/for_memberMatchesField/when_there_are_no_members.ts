// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { memberMatchesField } from '../memberMatchesField';

describe('when there are no members', () => {
    const result = memberMatchesField(undefined, 'email');

    it('should not match', () => result.should.be.false);
});
