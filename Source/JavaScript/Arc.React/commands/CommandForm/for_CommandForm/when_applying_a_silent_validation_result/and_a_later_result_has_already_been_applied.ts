// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render } from '@testing-library/react';
import { a_command_form_with_a_validation_probe, accepted, rejected } from './given/a_command_form_with_a_validation_probe';
import { given } from '../../../../given';

/**
 * Two runs, issued in order, answering out of order. The seam has to discard the older answer, and
 * it has to say so - a caller decides from the boolean whether the message that came with the result
 * still describes what the form holds, and there is nothing else it could ask.
 */
describe('when applying a silent validation result a later one has already been applied over', given(a_command_form_with_a_validation_probe, context => {
    let result: ReturnType<typeof render>;
    let appliedTheNewest: boolean;
    let appliedTheOvertaken: boolean;

    beforeEach(async () => {
        result = await context.renderForm();

        const overtaken = context.beginSilentValidation();
        const newest = context.beginSilentValidation();

        await context.settle();
        appliedTheNewest = context.applySilentValidationResult(accepted(), newest);
        appliedTheOvertaken = context.applySilentValidationResult(rejected(), overtaken);
        await context.settle();
    });

    afterEach(() => result.unmount());

    it('should apply the newest result', () => appliedTheNewest.should.be.true);

    it('should report the overtaken result as not applied', () => appliedTheOvertaken.should.be.false);

    it('should not let the overtaken result decide validity', () => context.isValid!.should.be.true);
}));
