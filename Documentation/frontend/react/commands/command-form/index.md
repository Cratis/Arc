# CommandForm

The `CommandForm` component provides a declarative way to build forms for Arc commands with built-in validation, error handling, and field management.

## Overview

CommandForm simplifies working with Arc commands in React by:

- Automatically managing command state
- Providing type-safe field bindings
- Handling validation integration
- Supporting flexible customization
- Managing form lifecycle

## Basic Usage

```tsx
import { CommandForm } from '@cratis/arc/commands';
import { InputTextField } from '@cratis/arc/commands/fields';

class UserCommand extends Command {
    name = '';
    email = '';
}

function MyForm() {
    return (
        <CommandForm<UserCommand> command={UserCommand}>
            <InputTextField 
                value={c => c.name} 
                title="Name"
                placeholder="Enter your name" 
            />
            <InputTextField 
                value={c => c.email} 
                title="Email"
                type="email" 
                placeholder="Enter your email" 
            />
            <button type="submit">Submit</button>
        </CommandForm>
    );
}
```

## Props Reference

### CommandFormProps

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `command` | `Constructor<TCommand>` | Required | The command class to use for the form |
| `initialValues` | `Partial<TCommand>` | `undefined` | Initial values for the form fields |
| `currentValues` | `Partial<TCommand>` | `undefined` | Current values that will be merged with initial values |
| `showTitles` | `boolean` | `true` | Whether to show field titles automatically |
| `showErrors` | `boolean` | `true` | Whether to show field error messages automatically |
| `fieldContainerComponent` | `React.ComponentType<FieldContainerProps>` | `undefined` | Custom component for rendering field containers |
| `onFieldValidate` | `(command, fieldName, oldValue, newValue) => string \| undefined` | `undefined` | Custom field validation function |
| `onFieldChange` | `(command, fieldName, oldValue, newValue) => void` | `undefined` | Callback when a field value changes |
| `onBeforeExecute` | `(values) => values` | `undefined` | Transform values before command execution |

## Initial Values

Set initial values for the form:

```tsx
<CommandForm<UserCommand>
    command={UserCommand}
    initialValues={{
        name: 'John Doe',
        email: 'john@example.com',
        role: 'user'
    }}
>
    <InputTextField value={c => c.name} title="Name" />
    <InputTextField value={c => c.email} title="Email" />
    <SelectField value={c => c.role} title="Role" options={roles} />
</CommandForm>
```

## Children

CommandForm accepts any React elements as children. The following are treated specially:

- **Field components** - Components with `displayName` of `'CommandFormField'` are automatically bound to the command
- **CommandForm.Column** - Used for multi-column layouts
- **Other elements** - Headings, buttons, divs, etc. are rendered as-is in order

## See Also

- [Field Types](field-types.md) - Available field components
- [Validation](validation.md) - Integration with Arc validation
- [Customization](customization.md) - Custom titles, errors, and containers
- [Advanced Usage](advanced.md) - Layouts, hooks, and async data
