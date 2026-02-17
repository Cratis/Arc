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
            <div className="p-8 w-[600px] mx-auto">
                <h2 className="text-2xl font-bold mb-6">Simple Command Form with Validation</h2>
                <p className="mb-4 text-gray-600">
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
                    <InputTextField<SimpleCommand> value={c => c.name} title="Name" placeholder="Enter your name (min 3 chars)" />
                    <ValidationMessage<SimpleCommand> value={c => c.name} />
                    {validationState.errors.name && (
                        <div className="text-red-500 text-sm mt-1">{validationState.errors.name}</div>
                    )}
                    
                    <InputTextField<SimpleCommand> value={c => c.email} title="Email" type="email" placeholder="Enter your email" />
                    <ValidationMessage<SimpleCommand> value={c => c.email} />
                    {validationState.errors.email && (
                        <div className="text-red-500 text-sm mt-1">{validationState.errors.email}</div>
                    )}

                    <div className="mt-4 flex gap-2 items-center">
                        <button 
                            type="submit" 
                            disabled={!validationState.canSubmit}
                            className="px-4 py-2 bg-blue-600 text-white rounded disabled:bg-gray-400 disabled:cursor-not-allowed"
                        >
                            Submit
                        </button>
                        {!validationState.canSubmit && Object.keys(validationState.errors).length > 0 && (
                            <span className="text-sm text-orange-600">Please fix validation errors</span>
                        )}
                    </div>
                </CommandForm>
            </div>
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
            <div className="p-8 w-[800px] mx-auto">
                <h2 className="text-2xl font-bold mb-6">User Registration Form with Progressive Validation</h2>
                <p className="mb-4 text-gray-600">
                    This form validates progressively as you type. The submit button is enabled only when all validation passes.
                </p>
                
                {validationSummary.length > 0 && (
                    <div className="bg-orange-100 border border-orange-300 p-4 rounded-lg mb-4">
                        <strong className="text-orange-800">Validation Issues:</strong>
                        <ul className="mt-2 mb-0 text-sm">
                            {validationSummary.map((error, index) => (
                                <li key={index} className="text-orange-700">{error}</li>
                            ))}
                        </ul>
                    </div>
                )}
                
                <h3 className="text-xl font-semibold mb-4 mt-6">Account Information</h3>
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
                    <InputTextField<UserRegistrationCommand> value={c => c.username} title="Username" placeholder="Enter username" />
                    <ValidationMessage<UserRegistrationCommand> value={c => c.username} />
                    
                    <InputTextField<UserRegistrationCommand> value={c => c.email} title="Email Address" type="email" placeholder="Enter email" />
                    <ValidationMessage<UserRegistrationCommand> value={c => c.email} />
                    
                    <InputTextField<UserRegistrationCommand> value={c => c.password} title="Password" type="password" placeholder="Enter password" />
                    <ValidationMessage<UserRegistrationCommand> value={c => c.password} />
                    
                    <InputTextField<UserRegistrationCommand> value={c => c.confirmPassword} title="Confirm Password" type="password" placeholder="Confirm password" />
                    <ValidationMessage<UserRegistrationCommand> value={c => c.confirmPassword} />

                    <h3 className="text-xl font-semibold mb-0 mt-6">Personal Information</h3>
                    <NumberField<UserRegistrationCommand> value={c => c.age} title="Age" placeholder="Enter age" min={13} max={120} />
                    <ValidationMessage<UserRegistrationCommand> value={c => c.age} />
                    
                    <InputTextField<UserRegistrationCommand> value={c => c.birthDate} title="Birth Date" type="date" placeholder="Select birth date" />
                    <ValidationMessage<UserRegistrationCommand> value={c => c.birthDate} />
                    
                    <TextAreaField<UserRegistrationCommand> value={c => c.bio} title="Bio" placeholder="Tell us about yourself" rows={4} required={false} />
                    <ValidationMessage<UserRegistrationCommand> value={c => c.bio} />
                    
                    <InputTextField<UserRegistrationCommand> value={c => c.favoriteColor} title="Favorite Color" type="color" />
                    <ValidationMessage<UserRegistrationCommand> value={c => c.favoriteColor} />

                    <h3 className="text-xl font-semibold mb-0 mt-6">Preferences</h3>
                    <SelectField<UserRegistrationCommand>
                        value={c => c.role}
                        title="Role"
                        options={roleOptions} 
                        optionIdField="id" 
                        optionLabelField="name"
                        placeholder="Select a role"
                    />
                    <ValidationMessage<UserRegistrationCommand> value={c => c.role} />
                    
                    <RangeField<UserRegistrationCommand> value={c => c.experienceLevel} title="Experience Level" min={0} max={100} step={10} />
                    <ValidationMessage<UserRegistrationCommand> value={c => c.experienceLevel} />
                    
                    <CheckboxField<UserRegistrationCommand> value={c => c.agreeToTerms} title="Terms & Conditions" label="I agree to the terms and conditions" />
                    <ValidationMessage<UserRegistrationCommand> value={c => c.agreeToTerms} />
                </CommandForm>

                <div className="flex gap-2 mt-6 items-center">
                    <button 
                        onClick={handleSubmit} 
                        disabled={!canSubmit}
                        className="px-4 py-2 bg-green-600 text-white rounded disabled:bg-gray-400 disabled:cursor-not-allowed"
                    >
                        Submit
                    </button>
                    <button 
                        onClick={() => setResult('')}
                        className="px-4 py-2 bg-gray-500 text-white rounded"
                    >
                        Cancel
                    </button>
                    {!canSubmit && (
                        <span className="text-sm text-orange-600 ml-2">Complete required fields with valid data</span>
                    )}
                </div>

                {result && (
                    <div className="bg-green-100 p-4 rounded-lg mt-4 border border-green-300">
                        <p className="text-green-800 font-semibold m-0">{result}</p>
                    </div>
                )}
            </div>
        );
    }
};
