// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render } from '@testing-library/react';
import { a_command_form_with_a_validation_probe, accepted, rejected } from './given/a_command_form_with_a_validation_probe';
import { given } from '../../../../given';

/**
 * The other direction, and the reason the guard cannot simply prefer whatever arrived first. Nothing
 * was overtaken here - the runs answer in the order they were issued - so both results are current
 * when they land and both have to be applied.
 */
describe('when applying silent validation results that answer in the order they were issued', given(a_command_form_with_a_validation_probe, context => {
    let result: ReturnType<typeof render>;
    let appliedTheFirst: boolean;
    let appliedTheSecond: boolean;

    beforeEach(async () => {
        result = await context.renderForm();

        const first = context.beginSilentValidation();
        const second = context.beginSilentValidation();

        await context.settle();
        appliedTheFirst = context.applySilentValidationResult(rejected(), first);
        appliedTheSecond = context.applySilentValidationResult(accepted(), second);
        await context.settle();
    });

    afterEach(() => result.unmount());

    it('should apply the first result', () => appliedTheFirst.should.be.true);

    it('should apply the second result', () => appliedTheSecond.should.be.true);

    it('should let the newest result decide validity', () => context.isValid!.should.be.true);
}));
