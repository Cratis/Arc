// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResultWithState } from '../../QueryResultWithState';

describe('when creating initial with valid default value', () => {
    const defaultValue = [{ id: '1', name: 'test' }];
    const result = QueryResultWithState.initial(defaultValue);

    it('should use the provided default value', () => result.data.should.equal(defaultValue));
    it('should have data', () => result.hasData.should.be.true);
    it('should be performing', () => result.isPerforming.should.be.true);
});
