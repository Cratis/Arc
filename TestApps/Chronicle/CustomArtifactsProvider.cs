// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Chronicle;

public class CustomArtifactsProvider : IClientArtifactsProvider
{
    public IEnumerable<Type> EventTypes => throw new NotImplementedException();

    public IEnumerable<Type> Projections => throw new NotImplementedException();

    public IEnumerable<Type> ModelBoundProjections => throw new NotImplementedException();

    public IEnumerable<Type> Reactors => throw new NotImplementedException();

    public IEnumerable<Type> Reducers => throw new NotImplementedException();

    public IEnumerable<Type> ReactorMiddlewares => throw new NotImplementedException();

    public IEnumerable<Type> ComplianceForTypesProviders => throw new NotImplementedException();

    public IEnumerable<Type> ComplianceForPropertiesProviders => throw new NotImplementedException();

    public IEnumerable<Type> AdditionalEventInformationProviders => throw new NotImplementedException();

    public IEnumerable<Type> ConstraintTypes => throw new NotImplementedException();

    public IEnumerable<Type> UniqueConstraints => throw new NotImplementedException();

    public IEnumerable<Type> UniqueEventTypeConstraints => throw new NotImplementedException();

    public IEnumerable<Type> RemoveConstraintEventTypes => throw new NotImplementedException();

    public IEnumerable<Type> EventSeeders => throw new NotImplementedException();

    public IEnumerable<Type> EventTypeMigrators => throw new NotImplementedException();

    public void Initialize() => throw new NotImplementedException();
}