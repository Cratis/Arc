// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResultWithState } from '../../QueryResultWithState';

describe('when creating empty with undefined default value', () => {
    const result = QueryResultWithState.empty<string[]>(undefined as unknown as string[]);

    it('should have undefined data since undefined defaultValue is passed through', () => (result.data === undefined).should.be.true);
    it('should not have data', () => result.hasData.should.be.false);
    it('should not be performing', () => result.isPerforming.should.be.false);
});
