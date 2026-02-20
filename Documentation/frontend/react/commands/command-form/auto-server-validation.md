# Auto Server Validation

CommandForm can automatically trigger server-side validation when all client-side validations pass. This provides real-time feedback for server-side validation rules (like checking if a username is already taken) without requiring an explicit form submission.

## Overview

Auto server validation is designed for scenarios where you need to validate data against server-side business rules while users are filling out a form. Instead of waiting for form submission to discover that a username is taken or an email is already registered, users get immediate feedback as they type.

**Key features**:

- Automatically calls `command.validate()` when all client validations pass
- Configurable throttling to prevent excessive server calls
- Seamlessly integrates with CommandForm's existing validation system
- Works with any backend validation endpoint

## Basic Usage

Enable auto server validation by setting the `autoServerValidate` prop:

```tsx
<CommandForm<RegisterUser> 
    command={RegisterUser} 
    validateOn="change"
    autoServerValidate={true}
>
    <InputTextField<RegisterUser> value={c => c.username} title="Username" required />
    <InputTextField<RegisterUser> value={c => c.email} type="email" title="Email" required />
</CommandForm>
```

When enabled, the form will automatically call the command's `validate()` method (which makes an HTTP request to `/api/{command-route}/validate`) whenever:

1. All client-side validations have passed
2. The command data changes
3. Any pending throttle timer has expired (if configured)

## Validation Flow

```flow
User types → Client validation runs → All fields valid? 
    ↓ Yes
Throttle timer starts (if configured) → Timer expires → HTTP POST /validate
    ↓
Server validates → Returns errors → Form displays errors
```

If the user continues typing before the throttle timer expires, the timer resets and starts over.

## Throttling

To avoid excessive server calls during rapid typing, use the `autoServerValidateThrottle` prop to delay validation:

```tsx
<CommandForm<RegisterUser> 
    command={RegisterUser} 
    validateOn="change"
    autoServerValidate={true}
    autoServerValidateThrottle={500}  // Wait 500ms after typing stops
>
    <InputTextField<RegisterUser> value={c => c.username} title="Username" required />
    <InputTextField<RegisterUser> value={c => c.email} type="email" title="Email" required />
</CommandForm>
```

### Throttle Values

The throttle value is in milliseconds:

- **500 (default)**: Good balance between responsiveness and server load - recommended for most forms
- **300**: More responsive but more server calls
- **1000**: Less responsive but fewer server calls
- **0**: No throttle, validation occurs immediately (not recommended for `validateOn="change"`)

### How Throttling Works

1. User makes a change that passes all client validations
2. Timer starts counting down from the throttle value
3. If user makes another change before timer expires:
   - Previous timer is cancelled
   - New timer starts from the beginning
4. When timer expires without interruption:
   - Server validation is triggered
5. If server returns errors:
   - Errors are displayed in the form
   - Timer resets for next validation

**Example**: With a 500ms throttle, typing "<john@example.com>" character by character:

- Without throttle: 17 server calls (one per character)
- With throttle: 1 server call (after user stops typing for 500ms)

## Backend Implementation

For auto server validation to work, your backend must implement a validation endpoint that returns validation errors without executing the command.

### C# Example

```csharp
public record RegisterUser(string Username, string Email, string Password);

public class RegisterUserValidator : CommandValidator<RegisterUser>
{
    private readonly IUserRepository _users;
    
    public RegisterUserValidator(IUserRepository users)
    {
        _users = users;
        
        RuleFor(x => x.Username)
            .NotEmpty()
            .MinLength(3)
            .MaxLength(20)
            .MustAsync(async (username, ct) => !await _users.UsernameExists(username, ct))
            .WithMessage("This username is already taken");
            
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MustAsync(async (email, ct) => !await _users.EmailExists(email, ct))
            .WithMessage("This email address is already registered");
            
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinLength(8)
            .WithMessage("Password must be at least 8 characters");
    }
}
```

The framework automatically exposes the validation endpoint at `/api/register-user/validate`. When called, it:

1. Deserializes the command from the request
2. Runs all validation rules
3. Returns errors without executing the command handler

### Validation Response

The validation endpoint returns a `CommandResult` with validation errors:

```json
{
  "isSuccess": false,
  "validationErrors": {
    "username": ["This username is already taken"],
    "email": ["This email address is already registered"]
  }
}
```

## Frontend Integration

The frontend automatically handles the validation response and displays errors:

```tsx
<CommandForm<RegisterUser> 
    command={RegisterUser} 
    validateOn="change"
    autoServerValidate={true}
    autoServerValidateThrottle={500}
>
    <InputTextField<RegisterUser> 
        value={c => c.username} 
        title="Username" 
        placeholder="Choose a username"
        required 
        minLength={3}
        maxLength={20}
    />
    {/* Error appears here: "This username is already taken" */}
    
    <InputTextField<RegisterUser> 
        value={c => c.email} 
        type="email" 
        title="Email" 
        placeholder="Enter your email"
        required 
    />
    {/* Error appears here: "This email address is already registered" */}
    
    <InputTextField<RegisterUser> 
        value={c => c.password} 
        type="password" 
        title="Password" 
        placeholder="Min 8 characters"
        required 
        minLength={8}
    />
    {/* Error appears here: "Password must be at least 8 characters" */}
</CommandForm>
```

## When to Use Auto Server Validation

### Good Use Cases

Auto server validation is ideal for:

- **Uniqueness checks**: Username, email, phone number availability
- **External validation**: Coupon codes, invite codes, API keys
- **Domain validation**: Domain name availability for SaaS products
- **Real-time quotas**: Check if user has reached limits
- **Complex business rules**: Rules that require database lookups or external services

### When to Avoid

Consider manual validation (validate on submit) instead when:

- **Slow endpoints**: Validation takes more than 1 second
- **High server load**: Validation is expensive (complex queries, external APIs)
- **Many interdependent fields**: Better to validate entire form at once
- **Sequential validation**: One field's validation depends on another's result
- **Privacy concerns**: You don't want to reveal information before submission

## Performance Considerations

### 1. Use Appropriate Throttle Values

```tsx
// Good: Balanced throttle for username checking
<CommandForm autoServerValidate autoServerValidateThrottle={500}>

// Avoid: No throttle with validateOn="change"
<CommandForm autoServerValidate autoServerValidateThrottle={0} validateOn="change">

// Consider: Longer throttle for expensive validation
<CommandForm autoServerValidate autoServerValidateThrottle={1000}>
```

### 2. Optimize Backend Validators

Keep validation queries fast:

```csharp
// Good: Indexed lookup
public async Task<bool> UsernameExists(string username, CancellationToken ct)
{
    return await _db.Users
        .Where(u => u.Username == username)  // Username column should be indexed
        .AnyAsync(ct);
}

// Avoid: Full table scan or complex joins
public async Task<bool> UsernameExists(string username, CancellationToken ct)
{
    var allUsers = await _db.Users.Include(u => u.Profile).ToListAsync(ct);
    return allUsers.Any(u => u.Username == username);
}
```

### 3. Implement Rate Limiting

Protect your validation endpoints from abuse:

```csharp
[HttpPost("validate")]
[RateLimitAttribute(MaxRequests = 10, TimeWindowSeconds = 60)]
public async Task<CommandResult> Validate([FromBody] RegisterUser command)
{
    // Validation logic
}
```

### 4. Cache Validation Results

For expensive validations, consider caching:

```csharp
public class RegisterUserValidator : CommandValidator<RegisterUser>
{
    private readonly IUserRepository _users;
    private readonly IMemoryCache _cache;
    
    public RegisterUserValidator(IUserRepository users, IMemoryCache cache)
    {
        _users = users;
        _cache = cache;
        
        RuleFor(x => x.Username)
            .MustAsync(async (username, ct) => 
            {
                var cacheKey = $"username_exists_{username}";
                if (_cache.TryGetValue(cacheKey, out bool exists))
                {
                    return !exists;
                }
                
                exists = await _users.UsernameExists(username, ct);
                _cache.Set(cacheKey, exists, TimeSpan.FromMinutes(5));
                return !exists;
            })
            .WithMessage("This username is already taken");
    }
}
```

### 5. Combine with Client Validation

Always validate client-side first to reduce unnecessary server calls:

```tsx
<CommandForm 
    autoServerValidate 
    autoServerValidateThrottle={500}
    validateOn="change"  // Client validation on every change
>
    <InputTextField 
        value={c => c.username}
        required              // Client-side required check
        minLength={3}        // Client-side length check
        maxLength={20}       // Client-side length check
        pattern="[a-zA-Z0-9_]+"  // Client-side pattern check
    />
    {/* Server validation only runs after client validation passes */}
</CommandForm>
```

## Complete Example

Here's a full registration form with auto server validation:

```tsx
import { CommandForm, InputTextField } from '@cratis/applications-react/commands';
import { RegisterUser } from './commands';

function RegistrationForm() {
    return (
        <div className="registration-form">
            <h2>Create Your Account</h2>
            
            <CommandForm<RegisterUser> 
                command={RegisterUser} 
                validateOn="change"
                autoServerValidate={true}
                autoServerValidateThrottle={500}
                onSuccess={() => {
                    console.log('Registration successful!');
                    // Redirect to dashboard
                }}
            >
                <InputTextField<RegisterUser> 
                    value={c => c.username} 
                    title="Username" 
                    placeholder="Choose a unique username"
                    required 
                    minLength={3}
                    maxLength={20}
                    pattern="[a-zA-Z0-9_]+"
                />
                <small>3-20 characters, letters, numbers, and underscores only</small>
                
                <InputTextField<RegisterUser> 
                    value={c => c.email} 
                    type="email" 
                    title="Email Address" 
                    placeholder="your.email@example.com"
                    required 
                />
                <small>We'll send a verification email</small>
                
                <InputTextField<RegisterUser> 
                    value={c => c.password} 
                    type="password" 
                    title="Password" 
                    placeholder="Min 8 characters"
                    required 
                    minLength={8}
                />
                <small>At least 8 characters</small>
                
                <button type="submit">Create Account</button>
            </CommandForm>
            
            <p className="help-text">
                Your username and email will be checked for availability as you type.
                Please wait for validation to complete before submitting.
            </p>
        </div>
    );
}
```

### What Happens

1. User types username: `"john"`
   - Client validation: ✓ Passes (3 chars, valid pattern)
   - Throttle: 500ms timer starts

2. User continues typing: `"john_"`
   - Client validation: ✓ Passes (5 chars, valid pattern)
   - Throttle: Previous timer cancelled, new 500ms timer starts

3. User continues typing: `"john_smith"`
   - Client validation: ✓ Passes (10 chars, valid pattern)
   - Throttle: Previous timer cancelled, new 500ms timer starts

4. User stops typing for 500ms
   - Server validation: HTTP POST /api/register-user/validate
   - Response: `{ "validationErrors": { "username": ["This username is already taken"] } }`
   - Form: Error displayed below username field

5. User changes username to `"john_smith_2024"`
   - Client validation: ✓ Passes
   - Throttle: 500ms timer starts
   - User stops typing for 500ms
   - Server validation: HTTP POST /api/register-user/validate
   - Response: `{ "isSuccess": true }`
   - Form: No errors, username is available

## Integration with Other Validation Props

Auto server validation works alongside other validation props:

```tsx
<CommandForm 
    validateOn="change"              // When to run client validation
    validateAllFieldsOnChange={false} // Validate only changed field
    validateOnInit={false}           // Don't validate empty form
    autoServerValidate={true}        // Enable server validation
    autoServerValidateThrottle={500} // Throttle server calls
>
    {/* fields */}
</CommandForm>
```

**Interaction**:

- `validateOn` controls when client validation runs
- `autoServerValidate` controls whether server validation runs automatically
- Server validation only triggers when ALL client validations pass
- `autoServerValidateThrottle` delays server validation, not client validation

## Best Practices

1. **Set reasonable throttle values**: 300-500ms for most forms, 1000ms+ for expensive validation
2. **Optimize backend validators**: Use database indexes, caching, and efficient queries
3. **Always validate client-side first**: Reduce server load by catching simple errors client-side
4. **Provide visual feedback**: Show loading state during server validation (see [Customization](./customization.md))
5. **Handle errors gracefully**: Display clear, actionable error messages
6. **Implement rate limiting**: Protect validation endpoints from abuse
7. **Consider user experience**: Don't validate too aggressively - let users finish typing
8. **Test throttle behavior**: Verify throttle works correctly with rapid typing
9. **Monitor server load**: Track validation endpoint performance and adjust throttle if needed
10. **Document validation rules**: Make it clear what server-side rules exist

## See Also

- [Validation](./validation.md) - Client-side validation options
- [Backend Command Validation](../../../backend/commands/validation.md) - Implementing validators
- [Customization](./customization.md) - Customizing validation display
- [Form Lifecycle](./form-lifecycle.md) - Understanding form state
