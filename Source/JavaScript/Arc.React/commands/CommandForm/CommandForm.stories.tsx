// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useState } from 'react';
import { Meta, StoryObj } from '@storybook/react';
import { CommandForm } from './CommandForm';
import { UserRegistrationCommand } from './UserRegistrationCommand';
import { 
    InputTextField, 
    NumberField, 
    TextAreaField, 
    CheckboxField, 
    RangeField,
    SelectField 
} from './fields';
import { Command, CommandValidator } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import { StoryContainer, StoryBadge } from '../../stories';
import '@cratis/arc/validation';

const meta: Meta<typeof CommandForm> = {
    title: 'CommandForm/CommandForm',
    component: CommandForm,
};

export default meta;
type Story = StoryObj<typeof CommandForm>;

// Simple command for the default story with validation
class SimpleCommand extends Command {
    readonly route: string = '/api/simple';
    readonly validation: CommandValidator = new SimpleCommandValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('name', String),
        new PropertyDescriptor('email', String),
    ];

    name = '';
    email = '';

    constructor() {
        super(Object, false);
    }

    get requestParameters(): string[] {
        return [];
    }

    get properties(): string[] {
        return ['name', 'email'];
    }
}

class SimpleCommandValidator extends CommandValidator<SimpleCommand> {
    constructor() {
        super();
        this.ruleFor(c => c.name).notEmpty().minLength(3);
        this.ruleFor(c => c.email).notEmpty().emailAddress();
    }
}

const roleOptions = [
    { id: 'user', name: 'User' },
    { id: 'admin', name: 'Administrator' },
    { id: 'moderator', name: 'Moderator' }
];

export const Default: Story = {
    render: () => {
        const [validationState, setValidationState] = useState<{
            errors: Record<string, string>;
            canSubmit: boolean;
        }>({ errors: {}, canSubmit: false });

        return (
            <StoryContainer size="sm" asCard>
                <h2>Simple Command Form with Validation</h2>
                <p>
                    This form demonstrates validation on blur. Fields are validated when you leave them.
                </p>
                <CommandForm<SimpleCommand>
                    command={SimpleCommand}
                    initialValues={{
                        name: '',
                        email: '',
                    }}
                    onFieldChange={async (command, fieldName) => {
                        // Validate on blur pattern
                        const result = await command.validate();
                        
                        if (!result.isValid) {
                            const fieldError = result.validationResults.find(
                                v => v.members.includes(fieldName)
                            );
                            
                            if (fieldError) {
                                setValidationState(prev => ({
                                    errors: { ...prev.errors, [fieldName]: fieldError.message },
                                    canSubmit: false
                                }));
                            }
                        } else {
                            setValidationState(prev => {
                                const { [fieldName]: removed, ...rest } = prev.errors;
                                return { errors: rest, canSubmit: true };
                            });
                        }
                    }}
                >
                    <InputTextField<SimpleCommand> 
                        value={c => c.name} 
                        title="Name"
                        placeholder="Enter your name (min 3 chars)" 
                    />
                    {validationState.errors.name && (
                        <div style={{ color: 'var(--color-error)', fontSize: '0.875rem', marginTop: '0.25rem', marginBottom: '1rem' }}>
                            {validationState.errors.name}
                        </div>
                    )}
                    
                    <InputTextField<SimpleCommand> 
                        value={c => c.email} 
                        title="Email"
                        type="email" 
                        placeholder="Enter your email" 
                    />
                    {validationState.errors.email && (
                        <div style={{ color: 'var(--color-error)', fontSize: '0.875rem', marginTop: '0.25rem', marginBottom: '1rem' }}>
                            {validationState.errors.email}
                        </div>
                    )}

                    <div style={{ marginTop: '1.5rem', display: 'flex', gap: '0.5rem', alignItems: 'center', flexWrap: 'wrap' }}>
                        <button 
                            type="submit" 
                            disabled={!validationState.canSubmit}
                        >
                            Submit
                        </button>
                        {!validationState.canSubmit && Object.keys(validationState.errors).length > 0 && (
                            <StoryBadge variant="warning">Please fix validation errors</StoryBadge>
                        )}
                    </div>
                </CommandForm>
            </StoryContainer>
        );
    }
};

export const UserRegistration: Story = {
    render: () => {
        const [result, setResult] = useState<string>('');
        const [canSubmit, setCanSubmit] = useState(false);
        const [validationSummary, setValidationSummary] = useState<string[]>([]);

        const handleSubmit = () => {
            setResult('Form submitted successfully!');
        };

        return (
            <StoryContainer size="sm" asCard>
                <h1>User Registration Form</h1>
                <p>
                    This form validates progressively as you type. The submit button is enabled only when all validation passes.
                </p>
                
                {validationSummary.length > 0 && (
                    <div className="story-card" style={{ 
                        backgroundColor: 'rgba(245, 158, 11, 0.1)', 
                        borderColor: 'var(--color-warning)',
                        marginBottom: 'var(--space-lg)'
                    }}>
                        <strong style={{ color: 'var(--color-warning)' }}>Validation Issues:</strong>
                        <ul style={{ marginTop: 'var(--space-sm)', marginBottom: 0 }}>
                            {validationSummary.map((error, index) => (
                                <li key={index} style={{ color: 'var(--color-warning)' }}>{error}</li>
                            ))}
                        </ul>
                    </div>
                )}
                
                <CommandForm<UserRegistrationCommand>
                    command={UserRegistrationCommand}
                    initialValues={{
                        username: '',
                        email: '',
                        password: '',
                        confirmPassword: '',
                        age: 18,
                        bio: '',
                        favoriteColor: '#3b82f6',
                        birthDate: '',
                        agreeToTerms: false,
                        experienceLevel: 50,
                        role: ''
                    }}
                    onFieldChange={async (command, fieldName, oldValue, newValue) => {
                        console.log(`Field ${fieldName} changed from`, oldValue, 'to', newValue);
                        
                        // Progressive validation - validate whenever fields change
                        const validationResult = await command.validate();
                        
                        if (!validationResult.isValid) {
                            setValidationSummary(validationResult.validationResults.map(v => v.message));
                            setCanSubmit(false);
                        } else {
                            setValidationSummary([]);
                            setCanSubmit(true);
                        }
                    }}
                >
                    <h3>Account Information</h3>
                    <InputTextField<UserRegistrationCommand> 
                        value={c => c.username} 
                        title="Username"
                        placeholder="Enter username" 
                    />
                    
                    <InputTextField<UserRegistrationCommand> 
                        value={c => c.email} 
                        title="Email Address"
                        type="email" 
                        placeholder="Enter email" 
                    />
                    
                    <InputTextField<UserRegistrationCommand> 
                        value={c => c.password} 
                        title="Password"
                        type="password" 
                        placeholder="Enter password" 
                    />
                    
                    <InputTextField<UserRegistrationCommand> 
                        value={c => c.confirmPassword} 
                        title="Confirm Password"
                        type="password" 
                        placeholder="Confirm password" 
                    />

                    <h3 style={{ marginTop: 'var(--space-2xl)', marginBottom: 0 }}>Personal Information</h3>
                    
                    <NumberField<UserRegistrationCommand> 
                        value={c => c.age} 
                        title="Age"
                        placeholder="Enter age" 
                        min={13} 
                        max={120} 
                    />
                    
                    <InputTextField<UserRegistrationCommand> 
                        value={c => c.birthDate} 
                        title="Birth Date"
                        type="date" 
                        placeholder="Select birth date" 
                    />
                    
                    <TextAreaField<UserRegistrationCommand> 
                        value={c => c.bio} 
                        title="Bio"
                        placeholder="Tell us about yourself" 
                        rows={4} 
                        required={false} 
                    />
                    
                    <InputTextField<UserRegistrationCommand> 
                        value={c => c.favoriteColor} 
                        title="Favorite Color"
                        type="color" 
                    />

                    <h3 style={{ marginTop: 'var(--space-2xl)', marginBottom: 0 }}>Preferences</h3>
                    
                    <SelectField<UserRegistrationCommand>
                        value={c => c.role}
                        title="Role"
                        options={roleOptions} 
                        optionIdField="id" 
                        optionLabelField="name"
                        placeholder="Select a role"
                    />
                    
                    <RangeField<UserRegistrationCommand> 
                        value={c => c.experienceLevel} 
                        title="Experience Level"
                        min={0} 
                        max={100} 
                        step={10} 
                    />
                    
                    <CheckboxField<UserRegistrationCommand> 
                        value={c => c.agreeToTerms} 
                        label="I agree to the terms and conditions" 
                    />
                </CommandForm>

                <div style={{ display: 'flex', gap: 'var(--space-md)', marginTop: 'var(--space-xl)', alignItems: 'center', flexWrap: 'wrap' }}>
                    <button 
                        onClick={handleSubmit} 
                        disabled={!canSubmit}
                        style={{ backgroundColor: canSubmit ? 'var(--color-success)' : undefined }}
                    >
                        Submit
                    </button>
                    <button 
                        onClick={() => setResult('')}
                        style={{ backgroundColor: 'var(--color-text-muted)' }}
                    >
                        Cancel
                    </button>
                    {!canSubmit && (
                        <StoryBadge variant="warning">Complete required fields with valid data</StoryBadge>
                    )}
                </div>

                {result && (
                    <div className="story-card" style={{ 
                        backgroundColor: 'rgba(34, 197, 94, 0.1)',
                        borderColor: 'var(--color-success)',
                        marginTop: 'var(--space-lg)'
                    }}>
                        <p style={{ color: 'var(--color-success)', fontWeight: 600, margin: 0 }}>{result}</p>
                    </div>
                )}
            </StoryContainer>
        );
    }
};
