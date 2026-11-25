// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ValidationResult } from '@cratis/arc/validation';

/** Validation failures not attributed to any registered field, without exception diagnostics. */
export interface ValidationSummaryProps {
    validationResults: readonly ValidationResult[];
}

/** Accessible default display for form-level validation failures. */
export function ValidationSummary({ validationResults }: ValidationSummaryProps) {
    const messages = [...new Set(validationResults.map(result => result.message).filter(message => message.trim().length > 0))];
    if (messages.length === 0) return null;
    return <div role='alert' className='command-form-validation-summary'>
        <ul>{messages.map(message => <li key={message}>{message}</li>)}</ul>
    </div>;
}
