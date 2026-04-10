// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResultWithState } from '../../QueryResultWithState';

describe('when creating empty with null default value', () => {
    const result = QueryResultWithState.empty<string[]>(null as unknown as string[]);

    it('should have null data since null defaultValue is passed through', () => (result.data === null).should.be.true);
    it('should not have data', () => result.hasData.should.be.false);
    it('should not be performing', () => result.isPerforming.should.be.false);
});
