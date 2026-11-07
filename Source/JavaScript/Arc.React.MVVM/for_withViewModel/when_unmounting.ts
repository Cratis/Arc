// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// @vitest-environment jsdom

import { renderViewModel, flushDisposal } from './given/a_view_model_with_cleanup.js';

[true, false].forEach(development => describe(`when unmounting with development ${development}`, () => {
    let context: ReturnType<typeof renderViewModel>;

    beforeEach(async () => {
        context = renderViewModel(development);
        context.rendered.unmount();
        if (!development) {
            context.detached.should.equal(1);
            context.disposals[0].should.have.been.calledOnce;
        }
        await flushDisposal();
    });

    afterEach(() => context.restore());

    it('should detach exactly once', () => context.detached.should.equal(1));
    it('should dispose the child container exactly once', () => context.disposals[0].should.have.been.calledOnce);
}));
