// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useState } from 'react';
import { Meta, StoryObj } from '@storybook/react';
import { CommandForm } from './CommandForm';
import { ValidationMessage } from './ValidationMessage';
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
                    <div style={{ marginBottom: 'var(--space-lg)', width: '100%' }}>
                        <label style={{ display: 'block', marginBottom: 'var(--space-sm)', fontWeight: 500 }}>
                            Name
                        </label>
                        <div style={{ width: '100%' }}>
                            <InputTextField<SimpleCommand> value={c => c.name} placeholder="Enter your name (min 3 chars)" />
                        </div>
                        <ValidationMessage<SimpleCommand> value={c => c.name} />
                        {validationState.errors.name && (
                            <div className="text-red-500 text-sm mt-1">{validationState.errors.name}</div>
                        )}
                    </div>
                    
                    <div style={{ marginBottom: 'var(--space-lg)', width: '100%' }}>
                        <label style={{ display: 'block', marginBottom: 'var(--space-sm)', fontWeight: 500 }}>
                            Email
                        </label>
                        <div style={{ width: '100%' }}>
                            <InputTextField<SimpleCommand> value={c => c.email} type="email" placeholder="Enter your email" />
                        </div>
                        <ValidationMessage<SimpleCommand> value={c => c.email} />
                        {validationState.errors.email && (
                            <div className="text-red-500 text-sm mt-1">{validationState.errors.email}</div>
                        )}
                    </div>

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
                
                <h3>Account Information</h3>
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
                    <div style={{ marginBottom: 'var(--space-lg)', width: '100%' }}>
                        <label style={{ display: 'block', marginBottom: 'var(--space-sm)', fontWeight: 500 }}>
                            Username
                        </label>
                        <div style={{ width: '100%' }}>
                            <InputTextField<UserRegistrationCommand> value={c => c.username} placeholder="Enter username" />
                        </div>
                        <ValidationMessage<UserRegistrationCommand> value={c => c.username} />
                    </div>
                    
                    <div style={{ marginBottom: 'var(--space-lg)', width: '100%' }}>
                        <label style={{ display: 'block', marginBottom: 'var(--space-sm)', fontWeight: 500 }}>
                            Email Address
                        </label>
                        <div style={{ width: '100%' }}>
                            <InputTextField<UserRegistrationCommand> value={c => c.email} type="email" placeholder="Enter email" />
                        </div>
                        <ValidationMessage<UserRegistrationCommand> value={c => c.email} />
                    </div>
                    
                    <div style={{ marginBottom: 'var(--space-lg)', width: '100%' }}>
                        <label style={{ display: 'block', marginBottom: 'var(--space-sm)', fontWeight: 500 }}>
                            Password
                        </label>
                        <div style={{ width: '100%' }}>
                            <InputTextField<UserRegistrationCommand> value={c => c.password} type="password" placeholder="Enter password" />
                        </div>
                        <ValidationMessage<UserRegistrationCommand> value={c => c.password} />
                    </div>
                    
                    <div style={{ marginBottom: 'var(--space-lg)', width: '100%' }}>
                        <label style={{ display: 'block', marginBottom: 'var(--space-sm)', fontWeight: 500 }}>
                            Confirm Password
                        </label>
                        <div style={{ width: '100%' }}>
                            <InputTextField<UserRegistrationCommand> value={c => c.confirmPassword} type="password" placeholder="Confirm password" />
                        </div>
                        <ValidationMessage<UserRegistrationCommand> value={c => c.confirmPassword} />
                    </div>

                    <h3 style={{ marginTop: 'var(--space-2xl)' }}>Personal Information</h3>
                    
                    <div style={{ marginBottom: 'var(--space-lg)', width: '100%' }}>
                        <label style={{ display: 'block', marginBottom: 'var(--space-sm)', fontWeight: 500 }}>
                            Age
                        </label>
                        <div style={{ width: '100%' }}>
                            <NumberField<UserRegistrationCommand> value={c => c.age} placeholder="Enter age" min={13} max={120} />
                        </div>
                        <ValidationMessage<UserRegistrationCommand> value={c => c.age} />
                    </div>
                    
                    <div style={{ marginBottom: 'var(--space-lg)', width: '100%' }}>
                        <label style={{ display: 'block', marginBottom: 'var(--space-sm)', fontWeight: 500 }}>
                            Birth Date
                        </label>
                        <div style={{ width: '100%' }}>
                            <InputTextField<UserRegistrationCommand> value={c => c.birthDate} type="date" placeholder="Select birth date" />
                        </div>
                        <ValidationMessage<UserRegistrationCommand> value={c => c.birthDate} />
                    </div>
                    
                    <div style={{ marginBottom: 'var(--space-lg)', width: '100%' }}>
                        <label style={{ display: 'block', marginBottom: 'var(--space-sm)', fontWeight: 500 }}>
                            Bio
                        </label>
                        <div style={{ width: '100%' }}>
                            <TextAreaField<UserRegistrationCommand> value={c => c.bio} placeholder="Tell us about yourself" rows={4} required={false} />
                        </div>
                        <ValidationMessage<UserRegistrationCommand> value={c => c.bio} />
                    </div>
                    
                    <div style={{ marginBottom: 'var(--space-lg)', width: '100%' }}>
                        <label style={{ display: 'block', marginBottom: 'var(--space-sm)', fontWeight: 500 }}>
                            Favorite Color
                        </label>
                        <div style={{ width: '100%' }}>
                            <InputTextField<UserRegistrationCommand> value={c => c.favoriteColor} type="color" />
                        </div>
                        <ValidationMessage<UserRegistrationCommand> value={c => c.favoriteColor} />
                    </div>

                    <h3 style={{ marginTop: 'var(--space-2xl)' }}>Preferences</h3>
                    
                    <div style={{ marginBottom: 'var(--space-lg)', width: '100%' }}>
                        <label style={{ display: 'block', marginBottom: 'var(--space-sm)', fontWeight: 500 }}>
                            Role
                        </label>
                        <div style={{ width: '100%' }}>
                            <SelectField<UserRegistrationCommand>
                                value={c => c.role}
                                options={roleOptions} 
                                optionIdField="id" 
                                optionLabelField="name"
                                placeholder="Select a role"
                            />
                        </div>
                        <ValidationMessage<UserRegistrationCommand> value={c => c.role} />
                    </div>
                    
                    <div style={{ marginBottom: 'var(--space-lg)', width: '100%' }}>
                        <label style={{ display: 'block', marginBottom: 'var(--space-sm)', fontWeight: 500 }}>
                            Experience Level
                        </label>
                        <div style={{ width: '100%' }}>
                            <RangeField<UserRegistrationCommand> value={c => c.experienceLevel} min={0} max={100} step={10} />
                        </div>
                        <ValidationMessage<UserRegistrationCommand> value={c => c.experienceLevel} />
                    </div>
                    
                    <div style={{ marginBottom: 'var(--space-lg)' }}>
                        <CheckboxField<UserRegistrationCommand> value={c => c.agreeToTerms} label="I agree to the terms and conditions" />
                        <ValidationMessage<UserRegistrationCommand> value={c => c.agreeToTerms} />
                    </div>
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
