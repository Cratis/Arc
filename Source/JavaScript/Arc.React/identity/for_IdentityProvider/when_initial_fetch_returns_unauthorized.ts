// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../given';
import { an_identity_provider } from './given/an_identity_provider';

describe('when initial fetch returns unauthorized', given(an_identity_provider, context => {
    let identityId: string;
    let identityName: string;
    let isSet: boolean;

    beforeEach(async () => {
        context.fetchStub = context.fetchHelper.stubFetch();
        context.fetchStub.resolves({
            ok: false,
            status: 401,
        } as Response);

        context.renderProvider();
        await context.waitForAsyncUpdates();

        identityId = context.capturedIdentity!.id;
        identityName = context.capturedIdentity!.name;
        isSet = context.capturedIdentity!.isSet;
    });

    afterEach(() => context.cleanup());

    it('should have empty identity id', () => identityId.should.equal(''));
    it('should have empty identity name', () => identityName.should.equal(''));
    it('should mark identity as not set', () => isSet.should.be.false);
    it('should have refresh method available', () => context.capturedIdentity!.refresh.should.be.a('function'));
}));
