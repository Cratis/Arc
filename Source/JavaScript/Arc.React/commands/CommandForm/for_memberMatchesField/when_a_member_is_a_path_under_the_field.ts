// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { memberMatchesField } from '../memberMatchesField';

describe('when a member is a path under the field', () => {
    const result = memberMatchesField(['email.Value'], 'email');

    it('should match', () => result.should.be.true);
});
