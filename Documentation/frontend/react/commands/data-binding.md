---
title: React command data binding and initial values
description: Bind React inputs to generated Cratis Arc commands, load initial values, and reset drafts while keeping the change-tracking baseline explicit.
---

Use the generated Cratis Arc command hook's setter to bind React inputs, and initial values to establish the baseline against which edits are compared. The command holds the draft you are editing and that baseline separately, so updating an input does not accept it as saved.

## Binding to command properties

Generated hooks return `[command, setValues, clearValues]`. Use `setValues({ property: value })` in an input's change handler: it updates the supplied properties and requests a React render. Assigning a command property directly notifies change tracking, but does not reliably rerender each edit while `hasChanges` remains true.

Pass explicit content objects to the setter. Generated public properties are accessors on the prototype, so `{ ...command }` does not copy the command's payload. Neither assignment nor the setter validates the draft automatically. Use [CommandForm](../command-form/index.md) if you want the form to manage validation.

## Initial values

| Operation | Effect |
| --- | --- |
| `GeneratedCommand.use(initialValues)` | Seeds values and baseline when the instance is created; later prop changes do not reseed it |
| `setValues(partialContent)` | Edits supplied properties and requests a React render; baseline unchanged |
| `command.setInitialValues(content)` | Sets supplied values and their baseline, including falsy values |
| `command.setInitialValuesFromCurrentValues()` | Accepts current properties as the baseline, including `''`, `0`, `false`, null, and undefined |
| `command.revertChanges()` | Restores values from the stored baseline |
| Hook `clearValues()` | Assigns undefined to properties and requests a render; does not establish a new baseline |
| `command.clear()` | Clears properties and baseline; distinct from the hook clearer |

`hasChanges` compares each property with its baseline using strict inequality. It can remain true across many keystrokes, so `[command.hasChanges]` is not a per-edit dependency for validation or autosave. It also does not tell you whether a server request succeeded; use the returned result for save feedback.

## Loading data from queries

Load the selected record before mounting its editor. Destructure the query tuple, check that the result is ready and successful, then read `.data`. Keeping loading separate from editing lets the editor take one initial snapshot rather than overwrite the user's draft whenever query data changes.

The example keys the query-owning loader by `userId` and checks the returned identity before mounting the editor. Keying only the editor could initialize it from a query result retained for the previous selection. It assumes generated `GetUserProfile` and `UpdateProfile` proxies with string `userId`, `firstName`, and `email` properties:

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

When the selected `userId` changes, React remounts both the loader and editor, discarding the previous draft. Ask for confirmation before changing selection if that draft must be preserved.

The editor consumes profile data on its initial mount. This loader also unmounts the editor while a query is performing, so refetching during editing can discard the draft. Decide whether to keep or discard edits before refreshing; [loading form data](../command-form/data-loading.md) covers the form-managed alternative.

## Resetting to initial values

Keep an explicit baseline object when you need a visible Reset action. `setValues(baseline)` restores the supplied properties and requests a render without changing the stored baseline. Include every property you intend to reset, and update that object after success if Reset should restore the last successful save.

When newly loaded content should replace both the draft and baseline, call `command.setInitialValues(content)` followed by `setValues(content)`. The second call makes the new values visible in React. Supply explicit properties rather than spreading the command instance.

## Execution baseline limitations

After successful execution, `execute()` accepts the submitted values as the new baseline, including falsy values such as `''`, `0`, and `false`. Unsuccessful execution leaves the baseline unchanged. If the user edits while a request is pending, success accepts the submitted snapshot. Later edits that differ from that snapshot remain changed.

The baseline is a local comparison point, not a fresh query result or a durable copy of the draft. Keep save feedback tied to the returned `CommandResult`, and reload query data when you need to display values produced by the server.

## Tracking changes across multiple commands

When several editors share a Save action, use a [command scope](./scope.md) to execute the changed commands registered in that scope. Nested scopes need their own coordination: the parent does not recursively execute them or aggregate their change and performing flags. Place a toolbar beneath the provider and consume it with `useCommandScope()`; `CommandScope` takes elements, not a render-function child.

For the full tuple and execution flow, see [React hook usage](./react-usage.md). To check each draft without displaying stale feedback, follow [command validation](./validation.md).
