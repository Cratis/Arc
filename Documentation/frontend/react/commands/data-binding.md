# Data Binding and Initial Values

Commands act as the data model for your forms and UI components. Understanding how to bind to command properties and manage initial values is essential for building responsive, change-tracked forms.

## Overview

The command holds properties that represent the payload of what you want to have happen. These properties are:
- Subject to validation rules
- Subject to business rules
- Often sourced from read models coming from [queries](../queries.md)
- Used for operations that are typically updates to existing data

## Binding to Command Properties

Instead of binding your frontend components to read models from queries, you can bind directly to command properties. This provides several benefits:

### Benefits

1. **Automatic Validation**: Validation rules running on the frontend execute automatically as values change
2. **Change Tracking**: The command tracks whether properties have changed from their original values
3. **Consistent State**: Single source of truth for form data
4. **Type Safety**: TypeScript types generated from backend command definitions

### Example

```typescript
import { UpdateProfile } from './generated/commands';

export const ProfileEditor = () => {
    const [command] = UpdateProfile.use({
        userId: currentUser.id,
        firstName: currentUser.firstName,
        lastName: currentUser.lastName,
        email: currentUser.email
    });

    return (
        <form>
            <input
                type="text"
                value={command.firstName}
                onChange={(e) => command.firstName = e.target.value}
            />
            <input
                type="text"
                value={command.lastName}
                onChange={(e) => command.lastName = e.target.value}
            />
            <input
                type="email"
                value={command.email}
                onChange={(e) => command.email = e.target.value}
            />
        </form>
    );
};
```

## Initial Values

Commands need initial values to enable change tracking. The `hasChanges` property compares current values against these initial values.

### Setting Initial Values

There are two recommended ways to set initial values:

**1. Via React Hook (Recommended):**

```typescript
const [command] = UpdateProfile.use({
    userId: 'user-123',
    firstName: 'John',
    lastName: 'Doe',
    email: 'john@example.com'
});
```

**2. Via setCommandValues Function:**

```typescript
const [command, setCommandValues] = UpdateProfile.use();

useEffect(() => {
    // Load data from API or query
    fetchUserProfile(userId).then(profile => {
        setCommandValues(profile);
    });
}, [userId]);
```

**3. Via setInitialValues (Advanced):**

```typescript
const [command] = UpdateProfile.use();

useEffect(() => {
    fetchUserProfile(userId).then(profile => {
        command.setInitialValues(profile);
    });
}, [userId]);
```

### When to Use Each Approach

- **React Hook Parameter**: When you have initial data available at component mount
- **setCommandValues**: When loading data asynchronously or from queries
- **setInitialValues**: Advanced scenarios requiring direct control (rare)

## Change Tracking

The `hasChanges` property automatically tracks whether any command property differs from its initial value.

### Basic Change Tracking

```typescript
const [command] = UpdateProfile.use({
    firstName: 'John',
    lastName: 'Doe'
});

console.log(command.hasChanges); // false

command.firstName = 'Jane';
console.log(command.hasChanges); // true

command.firstName = 'John'; // Reverted to original
console.log(command.hasChanges); // false
```

### Using hasChanges in UI

```typescript
export const ProfileEditor = () => {
    const [command] = UpdateProfile.use(currentProfile);

    const handleSave = async () => {
        if (command.hasChanges) {
            await command.execute();
        }
    };

    return (
        <form>
            {/* Form fields */}
            
            <button 
                onClick={handleSave}
                disabled={!command.hasChanges}
            >
                Save Changes
            </button>
            
            {command.hasChanges && (
                <div className="warning">
                    You have unsaved changes
                </div>
            )}
        </form>
    );
};
```

## Loading Data from Queries

A common pattern is loading data from a query and using it to initialize a command:

```typescript
import { GetUserProfile } from './generated/queries';
import { UpdateProfile } from './generated/commands';

export const ProfileEditor = ({ userId }: { userId: string }) => {
    const profile = GetUserProfile.use({ userId });
    const [command, setCommandValues] = UpdateProfile.use();

    useEffect(() => {
        if (profile) {
            setCommandValues({
                userId: profile.userId,
                firstName: profile.firstName,
                lastName: profile.lastName,
                email: profile.email
            });
        }
    }, [profile]);

    if (!profile) {
        return <div>Loading...</div>;
    }

    return (
        <form>
            <input
                value={command.firstName}
                onChange={(e) => command.firstName = e.target.value}
            />
            {/* More fields... */}
            
            <button disabled={!command.hasChanges}>
                Save
            </button>
        </form>
    );
};
```

## Tracking Changes Across Multiple Commands

When you have a component with sub-components that each work with different commands, you can track the aggregate `hasChanges` state using [Command Scope](./scope.md).

### Example Without Scope

```typescript
// Each command tracks its own changes
const [profileCommand] = UpdateProfile.use(profile);
const [settingsCommand] = UpdateSettings.use(settings);

// Need to manually check both
const hasAnyChanges = profileCommand.hasChanges || settingsCommand.hasChanges;
```

### Example With Scope

```typescript
import { CommandScope } from '@cratis/applications-react';

export const UserEditor = () => {
    return (
        <CommandScope>
            {(scope) => (
                <>
                    <ProfileForm />
                    <SettingsForm />
                    
                    <button disabled={!scope.hasChanges}>
                        Save All Changes
                    </button>
                </>
            )}
        </CommandScope>
    );
};
```

See [Command Scope](./scope.md) for complete documentation.

## Resetting to Initial Values

To reset a command to its initial state:

```typescript
const [command, setCommandValues] = UpdateProfile.use(initialProfile);

const handleReset = () => {
    setCommandValues(initialProfile);
};

// Or get fresh data
const handleRefresh = async () => {
    const freshProfile = await fetchUserProfile(userId);
    setCommandValues(freshProfile);
};
```

## Partial Updates

You can update only specific properties while keeping others unchanged:

```typescript
const [command, setCommandValues] = UpdateProfile.use({
    userId: 'user-123',
    firstName: 'John',
    lastName: 'Doe',
    email: 'john@example.com'
});

// Update only email
setCommandValues({
    ...command,
    email: 'newemail@example.com'
});
```

## Best Practices

1. **Always Provide Initial Values**: Set initial values to enable proper change tracking
2. **Use Queries as Source**: Load data from queries to initialize commands
3. **Check hasChanges**: Prevent unnecessary API calls by checking if data actually changed
4. **Use CommandScope**: For forms with multiple commands, use CommandScope to track aggregate state
5. **Reset After Save**: Consider resetting initial values after successful save to clear `hasChanges`
6. **Type Safety**: Leverage TypeScript to ensure initial values match command structure

## Example: Complete Edit Form

```typescript
import { GetUserProfile } from './generated/queries';
import { UpdateProfile } from './generated/commands';
import { useEffect, useState } from 'react';

export const UserProfileEditor = ({ userId }: { userId: string }) => {
    const profile = GetUserProfile.use({ userId });
    const [command, setCommandValues] = UpdateProfile.use();
    const [saved, setSaved] = useState(false);

    // Initialize command when profile loads
    useEffect(() => {
        if (profile) {
            setCommandValues(profile);
        }
    }, [profile]);

    const handleSave = async () => {
        if (!command.hasChanges) return;

        const result = await command.execute();
        if (result.isSuccess) {
            // Reset initial values to current values
            setCommandValues(command);
            setSaved(true);
            setTimeout(() => setSaved(false), 3000);
        }
    };

    const handleReset = () => {
        if (profile) {
            setCommandValues(profile);
        }
    };

    if (!profile) {
        return <div>Loading...</div>;
    }

    return (
        <div>
            <form>
                <div>
                    <label>First Name:</label>
                    <input
                        value={command.firstName}
                        onChange={(e) => command.firstName = e.target.value}
                    />
                </div>
                <div>
                    <label>Last Name:</label>
                    <input
                        value={command.lastName}
                        onChange={(e) => command.lastName = e.target.value}
                    />
                </div>
                <div>
                    <label>Email:</label>
                    <input
                        type="email"
                        value={command.email}
                        onChange={(e) => command.email = e.target.value}
                    />
                </div>
            </form>

            <div>
                <button 
                    onClick={handleSave}
                    disabled={!command.hasChanges}
                >
                    Save
                </button>
                <button 
                    onClick={handleReset}
                    disabled={!command.hasChanges}
                >
                    Reset
                </button>
            </div>

            {saved && <div className="success">Profile saved successfully!</div>}
            {command.hasChanges && <div className="info">You have unsaved changes</div>}
        </div>
    );
};
```

## See Also

- [Commands Overview](./index.md)
- [React Hook Usage](./react-usage.md)
- [Command Scope](./scope.md)
- [Validation](./validation.md)
- [Queries](../queries.md)
