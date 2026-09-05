// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { memberMatchesField } from '../memberMatchesField';

describe('when a member belongs to another field', () => {
    const result = memberMatchesField(['other.Value'], 'email');

    it('should not match', () => result.should.be.false);
});
