// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Cratis.Arc.Validation.for_DiscoverableModelValidatorProvider.given;

public class a_discoverable_model_validator_provider : Specification
{
    protected IDiscoverableValidators _discoverableValidators;
    protected DiscoverableModelValidatorProvider _provider;
    protected ModelValidatorProviderContext _context;

    void Establish()
    {
        _discoverableValidators = Substitute.For<IDiscoverableValidators>();
        _provider = new DiscoverableModelValidatorProvider(_discoverableValidators);

        var modelMetadataProvider = new EmptyModelMetadataProvider();
        var modelMetadata = modelMetadataProvider.GetMetadataForType(typeof(TestCommand));
        _context = new ModelValidatorProviderContext(modelMetadata, []);
    }

    public class TestCommand
    {
        public string Value { get; set; } = string.Empty;
    }
}
