// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { TestCommand } from '../for_CommandForm/TestCommand';
import { RadioButtonField } from './RadioButtonField';
import { RadioGroupField } from './RadioGroupField';

const validRadioButtonField = (
    <RadioButtonField
        value={(c: TestCommand) => c.name}
        setValue="Selected value"
        label="Selected value"
    />
);

const validRadioGroupField = (
    <RadioGroupField
        value={(c: TestCommand) => c.age}
        options={[
            { value: 18, label: '18' },
            { value: 21, label: '21' }
        ]}
    />
);

const invalidRadioButtonField = (
    <RadioButtonField
        value={(c: TestCommand) => c.age}
        // @ts-expect-error Radio button value should match the selected command property type
        setValue="Not a number"
        label="Invalid value"
    />
);

const invalidRadioGroupField = (
    <RadioGroupField
        value={(c: TestCommand) => c.age}
        options={[
            // @ts-expect-error Radio group option values should match the selected command property type
            { value: '18', label: '18' }
        ]}
    />
);

void validRadioButtonField;
void validRadioGroupField;
void invalidRadioButtonField;
void invalidRadioGroupField;
