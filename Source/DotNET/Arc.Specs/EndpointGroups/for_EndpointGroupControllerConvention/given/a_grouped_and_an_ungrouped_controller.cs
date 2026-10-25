// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.AspNetCore.EndpointGroups.for_EndpointGroupControllerConvention.given;

public class a_grouped_and_an_ungrouped_controller : Specification
{
    protected const int PublicPort = 5000;
    protected const int InternalPort = 5001;

    protected EndpointGroupMatcherPolicy _policy;
    protected ActionModel _groupedAction;
    protected ActionModel _ungroupedAction;
    protected Endpoint _groupedEndpoint;
    protected Endpoint _ungroupedEndpoint;
    protected CandidateSet _candidates;
    protected DefaultHttpContext _httpContext;

    void Establish()
    {
        var options = new ArcOptions();
        options.EndpointGroups.Assign("internal", InternalPort);
        _policy = new EndpointGroupMatcherPolicy(Options.Create(options));

        var application = new ApplicationModel();
        _groupedAction = AddController(application, typeof(InternalController));
        _ungroupedAction = AddController(application, typeof(PublicController));
        new EndpointGroupControllerConvention().Apply(application);

        _groupedEndpoint = EndpointFor(_groupedAction);
        _ungroupedEndpoint = EndpointFor(_ungroupedAction);
        _candidates = new CandidateSet([_groupedEndpoint, _ungroupedEndpoint], new RouteValueDictionary[2], [0, 0]);
        _httpContext = new DefaultHttpContext();
    }

    protected bool IsValid(Endpoint endpoint)
    {
        for (var i = 0; i < _candidates.Count; i++)
        {
            if (_candidates[i].Endpoint == endpoint)
            {
                return _candidates.IsValidCandidate(i);
            }
        }

        return false;
    }

    static ActionModel AddController(ApplicationModel application, Type controllerType)
    {
        var controller = new ControllerModel(controllerType.GetTypeInfo(), []);
        var action = new ActionModel(controllerType.GetMethod("Query")!, []);
        action.Selectors.Add(new SelectorModel());
        controller.Actions.Add(action);
        application.Controllers.Add(controller);
        return action;
    }

    static Endpoint EndpointFor(ActionModel action) =>
        new(_ => Task.CompletedTask, new EndpointMetadataCollection(action.Selectors.SelectMany(_ => _.EndpointMetadata)), null);

    [EndpointGroup("internal")]
    public class InternalController
    {
        public void Query() { }
    }

    public class PublicController
    {
        public void Query() { }
    }
}
