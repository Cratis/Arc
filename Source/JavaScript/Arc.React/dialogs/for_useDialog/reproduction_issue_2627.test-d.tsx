// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Reproduction case for issue #2627:
 * useDialog wrapper requires props at the JSX render site when TInput has required properties,
 * contradicting its own docs.
 *
 * Before the fix, the Wrapper type was FC<TInput>, which meant rendering <Wrapper />
 * with no props would fail to compile when TInput has required properties.
 *
 * After the fix, the Wrapper type is FC<Partial<TInput>>, allowing <Wrapper /> to render
 * with no props while still requiring the full input type via showDialog(input).
 */

import type { ReactElement } from 'react';
import { useDialog } from '../useDialog';
import { useDialogContext } from '../DialogContext';
import type { DialogResult } from '../DialogResult';

// Reproduction from the issue: a dialog component with required input properties
interface MyDialogInput {
    id: string;
}

const MyDialog = ({ id }: MyDialogInput): ReactElement => {
    const { closeDialog } = useDialogContext<void>();
    return (
        <div>
            Dialog with id: {id}
            <button onClick={() => closeDialog(DialogResult.Cancelled)}>Cancel</button>
        </div>
    );
};

// This is the scenario from the issue: Parent component using the dialog
// Before the fix, this would not compile because Wrapper is typed as FC<MyDialogInput>
// and MyDialogInput has required properties.
export const Parent = () => {
    const [Wrapper, show] = useDialog(MyDialog);
    return (
        <div>
            <button onClick={() => show({ id: '123' })}>Open</button>
            {/* ✅ This should compile now (would be TS2739 before the fix) */}
            <Wrapper />
            {/* ✅ This should also compile (partial props are fine) */}
            {/* <Wrapper id="test" /> */}
        </div>
    );
};
