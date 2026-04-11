// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../../IdentityProvider';
import { IIdentity } from '../../IIdentity';
import { an_identity_provider } from '../given/an_identity_provider';
import { given } from '../../../given';

describe('when refreshing with unauthorized response', given(an_identity_provider, context => {
    let result: IIdentity;

    beforeEach(async () => {
        context.fetchStub.resolves({
            ok: false,
            status: 401,
        } as Response);

        result = await IdentityProvider.refresh();
    });

    it('should return identity with isSet false', () => result.isSet.should.be.false);
    it('should return empty identity id', () => result.id.should.equal(''));
    it('should return empty identity name', () => result.name.should.equal(''));
    it('should return empty roles', () => result.roles.should.be.empty);
    it('should provide a refresh method', () => result.refresh.should.be.a('function'));
}));
