// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render } from '@testing-library/react';
import { a_command_form_with_a_validation_probe, accepted, rejected } from './given/a_command_form_with_a_validation_probe';
import { given } from '../../../../given';

/**
 * The branch a downstream custom field reaches: it writes its verdict through the context and has no
 * token to hand back, because it never claimed one. Nothing inside this package calls it that way,
 * which is exactly why it needs pinning here - it is public surface, and public surface nothing
 * exercises is where silent breakage lives.
 *
 * An untokened write cannot be ordered against anything, so it is taken as current, and every run
 * still in flight is stale from that point on. Without that, a run issued before it would land
 * afterwards and quietly overwrite it.
 */
describe('when applying a silent validation result that carries no token', given(a_command_form_with_a_validation_probe, context => {
    let result: ReturnType<typeof render>;
    let appliedTheUntokened: boolean;
    let appliedTheRunInFlight: boolean;

    beforeEach(async () => {
        result = await context.renderForm();

        const inFlight = context.beginSilentValidation();

        await context.settle();
        appliedTheUntokened = context.applySilentValidationResult(rejected());
        appliedTheRunInFlight = context.applySilentValidationResult(accepted(), inFlight);
        await context.settle();
    });

    afterEach(() => result.unmount());

    it('should apply the untokened result', () => appliedTheUntokened.should.be.true);

    it('should report the run it was already in flight behind as not applied', () => appliedTheRunInFlight.should.be.false);

    it('should keep the verdict the untokened result carried', () => context.isValid!.should.be.false);
}));
