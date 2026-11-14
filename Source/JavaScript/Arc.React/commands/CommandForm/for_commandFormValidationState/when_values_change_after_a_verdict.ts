// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    CommandFormValidationNotRunReason,
    describeDisabledServer,
    initialValidationState,
    invalidateFreshness,
    sameValidationState,
    type CommandFormValidationState,
} from '../commandFormValidationState.js';

describe('when values change after a verdict', () => {
    const current: CommandFormValidationState = {
        client: { pending: false, freshness: 'current', verdict: 'valid', validatedVersion: 1 },
        server: { pending: false, freshness: 'unvalidated', verdict: 'unknown' },
    };

    const result = invalidateFreshness(current);

    it('should make the current verdict stale', () => result.client.freshness.should.equal('stale'));

    it('should keep the verdict', () => result.client.verdict.should.equal('valid'));

    it('should keep the revision the verdict describes', () => result.client.validatedVersion!.should.equal(1));

    it('should leave an unvalidated channel unvalidated', () => result.server.freshness.should.equal('unvalidated'));

    it('should return the same instance when nothing was current', () =>
        invalidateFreshness(result).should.equal(result));

    it('should compare equal states by value', () =>
        sameValidationState(initialValidationState(), initialValidationState()).should.be.true);

    it('should tell different states apart', () => sameValidationState(current, result).should.be.false);
});

describe('when the form does not validate against the server', () => {
    const state = initialValidationState();

    it('should explain the unvalidated server channel', () =>
        describeDisabledServer(state, false).server.notRunReason!
            .should.equal(CommandFormValidationNotRunReason.ServerValidationDisabled));

    it('should explain nothing when the server is enabled', () =>
        describeDisabledServer(state, true).should.equal(state));

    it('should not overwrite a server channel that has a verdict', () => {
        const validated: CommandFormValidationState = {
            ...state,
            server: { pending: false, freshness: 'stale', verdict: 'valid', validatedVersion: 0 },
        };
        describeDisabledServer(validated, false).should.equal(validated);
    });
});
