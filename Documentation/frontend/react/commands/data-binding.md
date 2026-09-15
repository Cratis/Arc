---
title: Data binding and initial values
description: Edit generated command content without confusing React updates with the change-tracking baseline.
---

An edit screen needs two versions of its data: the draft and the baseline used to detect changes. A generated command holds both, but editing a property and accepting a new baseline are different operations.

## Binding to command properties

Generated hooks return `[command, setValues, clearValues]`. Prefer `setValues({ property: value })` for React input edits: it updates the named properties and requests a render. Raw assignment notifies command change tracking but does not reliably rerender every edit while `hasChanges` remains true. Neither raw assignment nor the setter runs validation automatically; [CommandForm](../command-form/index.md) adds form-driven validation.

Use explicit content objects. Generated public properties are prototype accessors, so `{ ...command }` is not a plain payload snapshot.

## Initial values

| Operation | Effect |
| --- | --- |
| `GeneratedCommand.use(initialValues)` | Seeds values and baseline when the instance is created; later prop changes do not reseed it |
| `setValues(partialContent)` | Edits supplied properties and requests a React render; baseline unchanged |
| `command.setInitialValues(content)` | Sets supplied values and their baseline, including falsy values |
| `command.setInitialValuesFromCurrentValues()` | Snapshots current **truthy** properties; skips `''`, `0`, `false`, null, and undefined |
| `command.revertChanges()` | Restores values from the stored baseline |
| Hook `clearValues()` | Assigns undefined to properties and requests a render; does not establish a new baseline |
| `command.clear()` | Clears properties and baseline; distinct from the hook clearer |

`hasChanges` is a comparison flag, not an edit revision or a saved-success flag. Do not use `[command.hasChanges]` as the dependency for per-keystroke validation or autosave.

## Loading data from queries

Destructure the query tuple, check readiness/success, and read `.data`. Avoid repeatedly reseeding an editor on observable updates: that can erase the user's draft. One simple pattern is to key the query-owning loader by the selected identity and mount its editor only after a successful result matches that identity. Keying only the editor can seed it with the previous selection's retained query result.

This illustrative pair assumes generated `GetUserProfile` and `UpdateProfile` proxies whose content has string `userId`, `firstName`, and `email` properties:

```tsx
import { useState } from 'react';
import { GetUserProfile } from './generated/GetUserProfile';
import { UpdateProfile } from './generated/UpdateProfile';

type Profile = { userId: string; firstName: string; email: string };

function Editor({ profile }: { profile: Profile }) {
    const [command, setValues] = UpdateProfile.use({
        userId: profile.userId, firstName: profile.firstName, email: profile.email
    });
    const [saving, setSaving] = useState(false);
    const [message, setMessage] = useState('');

    async function save() {
        setSaving(true);
        try {
            const result = await command.execute();
            setMessage(result.isSuccess ? 'Saved.' : 'Save failed; your draft is still displayed.');
        } finally {
            setSaving(false);
        }
    }

    return (
        <form onSubmit={event => { event.preventDefault(); void save(); }}>
            <input value={command.firstName ?? ''} disabled={saving}
                onChange={event => setValues({ firstName: event.target.value })} />
            <input value={command.email ?? ''} disabled={saving}
                onChange={event => setValues({ email: event.target.value })} />
            <button disabled={saving}>Save</button>
            <p role="status">{message}</p>
        </form>
    );
}

function ProfileLoader({ userId }: { userId: string }) {
    const [profile] = GetUserProfile.use({ userId });
    if (!profile.isReady || profile.isPerforming) return <p>Loading…</p>;
    if (!profile.isSuccess || profile.data.userId !== userId) {
        return <p>Could not load the selected profile.</p>;
    }
    return <Editor profile={profile.data} />;
}

export function ProfileEditor({ userId }: { userId: string }) {
    return <ProfileLoader key={userId} userId={userId} />;
}
```

Changing selection remounts the query loader and editor together, discarding the previous draft; confirm navigation first if unsaved edits must be preserved. The identity check also rejects a mismatched response. The editor consumes the incoming profile only on its initial mount. A deliberate refresh/reload must decide whether to discard edits first. For a form-managed alternative, see [loading form data](../command-form/data-loading.md).

## Resetting to initial values

If you retain an explicit baseline object, `setValues(baseline)` restores the draft without accepting a new baseline. To accept newly loaded content as the baseline and reliably render it, call `command.setInitialValues(content)` followed by `setValues(content)`. Supply explicit properties, not a spread command instance.

## Execution baseline limitations

Current `execute()` calls `setInitialValuesFromCurrentValues()` after every completed server request, **including unsuccessful results**. Client-validation short circuits do not reach that step. Combined with the truthy-only snapshot behavior, `hasChanges` is not a reliable “unsaved after failure” indicator for all values.

Keep authoritative save status in the returned `CommandResult` and preserve drafts independently when failure recovery matters. Success-only baseline acceptance and falsy-value snapshots are runtime follow-up candidates. Do not disable all retries solely because `hasChanges` became false after rejection.

## Tracking changes across multiple commands

A [command scope](./scope.md) tracks its own registered commands and can execute those with changes. It does not recursively execute nested scopes or aggregate their change/performing flags. Compose a toolbar under the provider with `useCommandScope()`; `CommandScope` does not accept a render-function child.

Continue with [React hook usage](./react-usage.md) and [validation](./validation.md).
