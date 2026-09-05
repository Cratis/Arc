// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { memberMatchesField } from '../memberMatchesField';

describe('when a member equals the field', () => {
    const result = memberMatchesField(['email'], 'email');

    it('should match', () => result.should.be.true);
});
