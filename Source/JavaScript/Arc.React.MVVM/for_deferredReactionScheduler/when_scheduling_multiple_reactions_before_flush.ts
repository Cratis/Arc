// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { deferredReactionScheduler } from '../deferredReactionScheduler';

describe('when scheduling multiple reactions before flush', () => {
    let runCount: number;

    beforeEach(async () => {
        runCount = 0;
        const run = () => { runCount++; };
        deferredReactionScheduler(run);
        deferredReactionScheduler(run);
        deferredReactionScheduler(run);
        await Promise.resolve();
    });

    it('should coalesce into a single microtask flush', () => runCount.should.equal(1));
});
