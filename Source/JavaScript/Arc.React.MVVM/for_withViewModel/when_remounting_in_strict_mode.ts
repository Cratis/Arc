// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// @vitest-environment jsdom

import { renderViewModel, flushDisposal } from './given/a_view_model_with_cleanup.js';

describe('when remounting in strict mode', () => {
    let context: ReturnType<typeof renderViewModel>;

    beforeEach(async () => {
        context = renderViewModel(true, true);
        await flushDisposal();
    });

    afterEach(async () => {
        context.rendered.unmount();
        await flushDisposal();
        context.restore();
    });

    it('should retain the view model', () => context.constructed.should.equal(1));
    it('should not detach during the simulated unmount', () => context.detached.should.equal(0));
    it('should not dispose the child container during the simulated unmount', () => context.disposals[0].should.not.have.been.called);
    it('should dispose exactly once on the real unmount', async () => {
        context.rendered.unmount();
        await flushDisposal();
        context.detached.should.equal(1);
        context.disposals[0].should.have.been.calledOnce;
    });
});
