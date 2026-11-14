// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render } from '@testing-library/react';
import { a_command_form_reporting_validation_state, accepted } from './given/a_command_form_reporting_validation_state.js';
import { given } from '../../../../given.js';

describe('when publishing validation state while a validation is in flight', given(a_command_form_reporting_validation_state, context => {
    let result: ReturnType<typeof render>;
    let pendingWhileInFlight: boolean;
    let pendingAfterSettling: boolean;
    let freshnessAfterSettling: string;
    let freshnessAfterEdit: string;
    let versionAfterSettling: number | undefined;

    beforeEach(async () => {
        result = await context.renderForm();

        const issue = context.beginSilentValidation('client');
        await context.act(() => { });
        pendingWhileInFlight = context.validation!.client.pending;

        await context.act(() => { context.applySilentValidationResult(accepted(), issue); });
        pendingAfterSettling = context.validation!.client.pending;
        freshnessAfterSettling = context.validation!.client.freshness;
        versionAfterSettling = context.validation!.client.validatedVersion;

        await context.act(() => context.setCommandValues({ name: 'Other' }));
        freshnessAfterEdit = context.validation!.client.freshness;
    });

    afterEach(() => result.unmount());

    it('should report the channel as pending while in flight', () => pendingWhileInFlight.should.be.true);

    it('should clear pending once the result is applied', () => pendingAfterSettling.should.be.false);

    it('should report the verdict as current once applied', () => freshnessAfterSettling.should.equal('current'));

    it('should record the revision the verdict describes', () => (versionAfterSettling !== undefined).should.be.true);

    it('should make the verdict stale when a value changes', () => freshnessAfterEdit.should.equal('stale'));

    it('should keep the verdict valid while stale', () => context.validation!.client.verdict.should.equal('valid'));

    it('should leave the server channel unvalidated', () => context.validation!.server.freshness.should.equal('unvalidated'));

    it('should explain that the server is not asked', () => context.validation!.server.notRunReason!.should.equal('serverValidationDisabled'));
}));
