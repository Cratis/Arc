// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Cratis.Arc.Commands;

/// <summary>
/// Represents an <see cref="IApplicationModelProvider"/> that adds /validate routes to controller-based command actions.
/// </summary>
public class CommandValidationRouteConvention : IApplicationModelProvider
{
    /// <inheritdoc/>
    public int Order => -990;

    /// <inheritdoc/>
    public void OnProvidersExecuting(ApplicationModelProviderContext context)
    {
        foreach (var controller in context.Result.Controllers)
        {
            foreach (var action in controller.Actions)
            {
                if (IsCommandAction(action))
                {
                    AddValidationSelector(action);
                }
            }
        }
    }

    /// <inheritdoc/>
    public void OnProvidersExecuted(ApplicationModelProviderContext context)
    {
    }

    bool IsCommandAction(ActionModel action)
    {
        if (action.ActionMethod?.IsCommand() != true)
        {
            return false;
        }

        var hasPostAttribute = action.Attributes.Any(a => a.GetType().Name == nameof(HttpPostAttribute));
        var actionNameIsPost = action.ActionName?.Equals(HttpMethod.Post.Method, StringComparison.OrdinalIgnoreCase) == true;

        return hasPostAttribute || actionNameIsPost;
    }

    void AddValidationSelector(ActionModel action)
    {
        foreach (var selector in action.Selectors.ToList())
        {
            if (selector.AttributeRouteModel is null) continue;

            var validationSelector = new SelectorModel(selector);
            var template = (selector.AttributeRouteModel.Template ?? string.Empty).TrimEnd('/');
            validationSelector.AttributeRouteModel = new AttributeRouteModel
            {
                Template = $"{template}/validate",
                Order = selector.AttributeRouteModel.Order,
                Name = selector.AttributeRouteModel.Name is not null
                    ? $"{selector.AttributeRouteModel.Name}Validate"
                    : null
            };

            action.Selectors.Add(validationSelector);
        }
    }
}
