// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { memberMatchesField } from '../memberMatchesField';

describe('when a member only shares a prefix with the field', () => {
    const result = memberMatchesField(['emailAddress'], 'email');

    it('should not match', () => result.should.be.false);
});
