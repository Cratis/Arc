// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expectTypeOf } from 'vitest';
import type { ReactElement } from 'react';
import { useDialog } from '../useDialog';
import type { DialogResponse } from '../DialogResponse';

// A dialog that expects an input type. It reads `closeDialog` from `useDialogContext`,
// so its declared props are exactly the input — no `closeDialog` prop, no cast.
type RegisterInput = { name: string };
const InputDialog = (props: RegisterInput): ReactElement => <span>{props.name}</span>;

// A dialog that takes no input at all.
const NoInputDialog = (): ReactElement => <span>No input</span>;

// --- Input dialog: TInput is inferred from the component, no cast required ---
{
    const [, showDialog] = useDialog(InputDialog);
    // `show` accepts the inferred input shape (optional, since a dialog may need no runtime props)...
    expectTypeOf(showDialog).parameter(0).toEqualTypeOf<RegisterInput | undefined>();
    // ...and resolves with the response tuple.
    expectTypeOf(showDialog).returns.resolves.toEqualTypeOf<DialogResponse<object>>();
    void showDialog({ name: 'Jane' });
}

// --- Input dialog with an explicit response type: response typed, input still checked ---
{
    type RegisterResponse = { id: string };
    const [, showDialog] = useDialog<RegisterResponse, RegisterInput>(InputDialog);
    expectTypeOf(showDialog).returns.resolves.toEqualTypeOf<DialogResponse<RegisterResponse>>();
    void showDialog({ name: 'Jane' });
}

// --- No-input dialog: show() is callable with no arguments ---
{
    const [, showDialog] = useDialog(NoInputDialog);
    void showDialog();
}

// --- Mismatched input type: must be a compile error ---
{
    // @ts-expect-error - a component expecting RegisterInput is not a ComponentType<{ age: number }>
    useDialog<object, { age: number }>(InputDialog);
}

// --- Mismatched show() argument: must be a compile error ---
{
    const [, showDialog] = useDialog(InputDialog);
    // @ts-expect-error - { age } is not assignable to the inferred RegisterInput
    void showDialog({ age: 42 });
}

// --- Wrapper can render with no props for a dialog with required input ---
{
    const [Wrapper, showDialog] = useDialog(InputDialog);
    // Even though InputDialog has required props (RegisterInput with `name: string`),
    // the wrapper should be renderable with no props since they are never read from the JSX site.
    // The actual input is supplied later via showDialog(input).
    // This should NOT be a type error:
    const element: ReactElement = <Wrapper />;
    // And rendering with partial props should also be fine:
    const partialElement: ReactElement = <Wrapper name="test" />;
    // But showDialog still requires the full input type:
    void showDialog({ name: 'Jane' });
}
