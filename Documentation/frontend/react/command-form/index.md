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
import { CommandForm } from '@cratis/arc.react/commands';
import { InputTextField } from '@cratis/arc.react/commands';

class UserCommand extends Command {
    name = '';
    email = '';
}

function MyForm() {
    return (
        <CommandForm command={UserCommand}>
            <InputTextField<UserCommand>
                value={c => c.name} 
                title="Name"
                placeholder="Enter your name" 
            />
            <InputTextField<UserCommand>
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

> **Note**: Field components require an explicit generic type parameter (e.g., `<InputTextField<UserCommand>>`) to ensure the `value` accessor function is properly typed. This provides full IntelliSense and type safety when writing `c => c.propertyName`.

## Props Reference

### CommandFormProps

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `command` | `Constructor<TCommand>` | Required | The command class to use for the form |
| `initialValues` | `Partial<TCommand>` | `undefined` | Initial values for the form fields |
| `currentValues` | `Partial<TCommand>` | `undefined` | Current values that will be merged with initial values |
| `showTitles` | `boolean` | `true` | Whether to show field titles automatically |
| `showErrors` | `boolean` | `true` | Whether to show automatic field errors and form-level exception feedback, including the custom exception renderer |
| `exceptionMessage` | `string` | `'An unexpected error occurred. Please try again.'` | Safe user-facing exception text; supports localization and preserves an explicitly empty string |
| `exceptionDisplayComponent` | `React.ComponentType<ExceptionDisplayProps>` | `undefined` | Replaces the default exception panel; receives only `{ message: string }` and is not invoked when `showErrors` is false |
| `fieldContainerComponent` | `React.ComponentType<FieldContainerProps>` | `undefined` | Custom component for rendering field containers |
| `fieldDecoratorComponent` | `React.ComponentType<FieldDecoratorProps>` | `undefined` | Custom component for decorating fields with icons and tooltips |
| `errorDisplayComponent` | `React.ComponentType<ErrorDisplayProps>` | `undefined` | Custom component for rendering field validation errors, not form-level exceptions |
| `tooltipComponent` | `React.ComponentType<TooltipWrapperProps>` | `undefined` | Custom component for rendering tooltips on field descriptions |
| `errorClassName` | `string` | `'p-error'` | CSS class name for error message elements |
| `iconAddonClassName` | `string` | `'p-inputgroup-addon'` | CSS class name for icon addon containers |
| `onFieldValidate` | `(command, fieldName, oldValue, newValue) => string \| undefined` | `undefined` | Custom field validation function |
| `onFieldChange` | `(command, fieldName, oldValue, newValue) => void` | `undefined` | Callback when field value changes |
| `onBeforeExecute` | `(values) => values` | `undefined` | Transform command values before execution |
| `onSuccess` | `(response: TResponse) => void` | `undefined` | Called when command executes successfully |
| `onFailed` | `(commandResult: ICommandResult<TResponse>) => void` | `undefined` | Called with the original result when command execution fails |
| `onException` | `(messages: string[], stackTrace: string) => void` | `undefined` | Called with original diagnostics when the command result has `hasExceptions: true` |
| `onUnauthorized` | `() => void` | `undefined` | Called when user is not authorized |
| `onValidationFailure` | `(validationResults: ValidationResult[]) => void` | `undefined` | Called when command fails validation |

Exception feedback uses safe text rather than raw server diagnostics. See [safe exception customization](./customization.md#safe-exception-feedback) for localization and replacement renderers, and [exception diagnostics and display](./form-lifecycle.md#exception-diagnostics-and-display) for callback and context behavior.

## Initial Values

Set initial values for the form:

```tsx
<CommandForm
    command={UserCommand}
    initialValues={{
        name: 'John Doe',
        email: 'john@example.com',
        role: 'user'
    }}
>
    <InputTextField<UserCommand> value={c => c.name} title="Name" />
    <InputTextField<UserCommand> value={c => c.email} title="Email" />
    <SelectField<UserCommand> value={c => c.role} title="Role" options={roles} />
</CommandForm>
```

## Children

CommandForm accepts any React elements as children. The following are treated specially:

- **Field components** - Components with `displayName` of `'CommandFormField'` are automatically bound to the command
- **CommandForm.Column** - Used for multi-column layouts
- **Other elements** - Headings, buttons, divs, etc. are rendered as-is in order

## See Also

- [Field Types](field-types/index.md) - Available field components
- [Validation](validation.md) - Integration with Arc validation
- [Customization](customization.md) - Custom titles, errors, and containers
- [Advanced Usage](advanced-patterns.md) - Layouts, hooks, and async data
