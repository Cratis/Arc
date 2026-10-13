// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { NotEmptyRule } from '../../NotEmptyRule.js';

describe('with mixed unicode whitespace', () => {
    it('should return one validation error', () => {
        const rule = new NotEmptyRule<{ value: string }>(t => t.value);
        rule.validate({ value: '\t \u3000\n' }, 'value').should.have.lengthOf(1);
    });
});
