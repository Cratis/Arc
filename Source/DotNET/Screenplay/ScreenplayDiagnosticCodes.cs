// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay;

/// <summary>
/// Holds the stable codes every <see cref="ScreenplayDiagnostic"/> is identified by.
/// </summary>
/// <remarks>
/// Codes are never reused and never renumbered - consumers suppress and group on them.
/// </remarks>
public static class ScreenplayDiagnosticCodes
{
    /// <summary>
    /// Source analysis has not produced a model.
    /// </summary>
    public const string AnalysisUnavailable = "SP0001";

    /// <summary>
    /// A slice declared nothing that can be expressed and was left out.
    /// </summary>
    public const string EmptySlice = "SP0002";

    /// <summary>
    /// A concept declared an enumeration without any values and was left out.
    /// </summary>
    public const string EnumWithoutValues = "SP0003";

    /// <summary>
    /// A projection produced no directives at all and was left out.
    /// </summary>
    public const string EmptyProjection = "SP0004";

    /// <summary>
    /// A projection property expression has no counterpart in the projection definition language.
    /// </summary>
    public const string UnmappableProjectionExpression = "SP0005";

    /// <summary>
    /// A projection key has no counterpart in the projection definition language.
    /// </summary>
    public const string UnmappableProjectionKey = "SP0006";

    /// <summary>
    /// A child or nested projection scope could not be expressed and was left out.
    /// </summary>
    public const string UnmappableProjectionScope = "SP0007";

    /// <summary>
    /// A join declared no property to key on and was left out.
    /// </summary>
    public const string UnmappableProjectionJoin = "SP0008";

    /// <summary>
    /// A reactor observed no events and was left out.
    /// </summary>
    public const string ReactorWithoutEvents = "SP0009";

    /// <summary>
    /// A concurrency scope narrowed nothing at all and was left out.
    /// </summary>
    public const string EmptyConcurrencyScope = "SP0010";

    /// <summary>
    /// A validation rule requires an operand it was not given, and was left out.
    /// </summary>
    public const string ValidationRuleWithoutOperand = "SP0011";

    /// <summary>
    /// A command handler produces events in a way that could not be read from the source.
    /// </summary>
    public const string UnmappableCommandProduction = "SP0012";

    /// <summary>
    /// A command handler's returned event source identity cannot be proven as an admitted destination.
    /// </summary>
    /// <remarks>
    /// Required scalar UUID concepts without validation can be generated and returned in ESM v7.
    /// Other tuple destinations remain unstated rather than inferred from the returned event.
    /// </remarks>
    public const string UnmappableEventSourceIdResult = "SP0013";

    /// <summary>
    /// An event declares generations, a tombstone or a compensation, none of which Screenplay has a counterpart for.
    /// </summary>
    public const string EventFeatureWithoutCounterpart = "SP0014";

    /// <summary>
    /// A projection declares something the projection definition language has no counterpart for.
    /// </summary>
    /// <remarks>
    /// Everything this reports is a construct within a projection that the language has no word for. A slice holding
    /// a second projection was once reported here too, because a slice used to declare at most one and the rest were
    /// turned away (Cratis/Screenplay#30); a slice now holds as many as its behavior needs, so they are all carried
    /// and there is nothing left to say about them.
    /// </remarks>
    public const string UnmappableProjectionConstruct = "SP0015";

    /// <summary>
    /// A validator declares a rule that could not be expressed declaratively.
    /// </summary>
    /// <remarks>
    /// A rule living in code, a chain rooted in something that does not name a property, and a message either put
    /// together while the request runs or following no rule to attach it to are all read as far as they go and then
    /// left out. A rule held to a <c>When</c> or an <c>Unless</c> is different: it is written down, but as though
    /// nothing held it, because a rule carries no condition of its own (Cratis/Screenplay#32). That is a difference
    /// between the document and the application rather than an omission from it, which is why the report names the
    /// condition the rule was held to rather than only the call carrying it.
    /// </remarks>
    public const string UnmappableValidationRule = "SP0016";

    /// <summary>
    /// A constraint declares a rule that is neither unique on a property nor unique on an event type.
    /// </summary>
    public const string UnmappableConstraint = "SP0017";

    /// <summary>
    /// An aggregate root produces events from code, which Screenplay has no counterpart for.
    /// </summary>
    public const string AggregateRootWithoutCounterpart = "SP0018";

    /// <summary>
    /// A query returns something that could not be expressed as a Screenplay type reference.
    /// </summary>
    public const string UnmappableQuery = "SP0019";

    /// <summary>
    /// A reducer folds events into a read model with code that cannot be stated.
    /// </summary>
    /// <remarks>
    /// A projection says what each event does to the read model it builds. A reducer says the same thing as code,
    /// and the language has no construct to fold one value into another (Cratis/Screenplay#39). The events it
    /// observes are read from its signatures and are real, so the read model is carried as a projection over those
    /// events while what the fold works out is left unsaid.
    /// </remarks>
    public const string ReducerWithoutCounterpart = "SP0020";

    /// <summary>
    /// An event the application refers to is declared neither by it nor by anything it references.
    /// </summary>
    /// <remarks>
    /// An event a referenced package declares is real and can be stated - an <c>import</c> names it and the compiler
    /// then reads it as an event that is known - so that case is written rather than reported. This is what is left:
    /// a name nothing at all resolves to, where inventing a declaration would describe an event the application does
    /// not have and staying silent would leave a document referring to something it never introduces.
    /// </remarks>
    public const string EventDeclaredOutsideCompilation = "SP0021";

    /// <summary>
    /// More than one query in a slice is declared under the same name.
    /// </summary>
    public const string AmbiguousQueryName = "SP0022";

    /// <summary>
    /// A namespace carries no structure to arrange the document by, so several levels take the same name.
    /// </summary>
    public const string NamespaceWithoutStructure = "SP0023";

    /// <summary>
    /// The source did not compile, and how far what was recovered from it can be relied on is what the severity says.
    /// </summary>
    /// <remarks>
    /// This is the one code whose severity is decided rather than fixed, because "the source did not compile" covers
    /// two outcomes that could not be further apart. A host handing over a compilation assembled without the compile
    /// items a build generates leaves every reference to a generated type unresolved - hundreds of errors, none of
    /// them anywhere near an artifact - while every command, event and reactor is read exactly as written. Calling
    /// that an error says something untrue about a document that is entirely correct, and makes the host throw it
    /// away.
    /// <para>
    /// So the severity follows how many artifacts were recovered from a declaration no compilation error sits inside.
    /// None - because nothing was recovered at all, or because every declaration something came out of is one the
    /// compiler could not make sense of - is an error, and nothing in the document is worth trusting. Any at all is a
    /// warning saying how many came through, because an artifact read from source the compiler accepted describes
    /// what that source states regardless of what failed elsewhere.
    /// </para>
    /// <para>
    /// As an error it suppresses <see cref="AnalysisUnavailable"/> and <see cref="DocumentDidNotCompile"/>, both for
    /// the same reason: a model recovered from symbols the compiler never accepted describes an application that does
    /// not exist, so an empty document and a rejected one are consequences of the broken build rather than defects of
    /// their own. As a warning it suppresses neither - it can never coincide with the first, since a warning is only
    /// reached when something was recovered, and suppressing the second would hand back a document the language
    /// rejects with nothing wrong reported.
    /// </para>
    /// </remarks>
    public const string SourceDidNotCompile = "SP0024";

    /// <summary>
    /// A user interface file sits alongside the source of a slice whose relationship to it is not certain.
    /// </summary>
    /// <remarks>
    /// Which file realizes a screen comes from where the file sits, so this reports every case where sitting there
    /// says less than usual: source spread over several folders, one folder holding the source of several slices,
    /// and two files claiming one screen name.
    /// </remarks>
    public const string AmbiguousScreenFile = "SP0025";

    /// <summary>
    /// A named authorization policy is referred to, but what it requires of the caller could not be recovered.
    /// </summary>
    /// <remarks>
    /// The name of a policy sits on the artifact, while what it requires sits where the application is composed.
    /// When the registration cannot be found or cannot be read, the document still declares the policy - a reference
    /// to something undeclared is a document that warns - but it declares the least it can say rather than a guess.
    /// </remarks>
    public const string PolicyRequirementsUnrecoverable = "SP0026";

    /// <summary>
    /// An event is produced from a branch guarded by the state an aggregate root holds.
    /// </summary>
    /// <remarks>
    /// A <c>produces when</c> condition compares the input of the command and nothing else, because the input is all
    /// a document knows about at the moment a command is issued. An aggregate root deciding on what it has already
    /// seen is a real decision with nowhere in the language to go, which is a different thing from a guard that could
    /// not be read - so it is reported apart from one, and the production is stated unconditionally.
    /// </remarks>
    public const string UnmappableAggregateStateCondition = "SP0027";

    /// <summary>
    /// The declarative body of a screen beyond the queries it binds is not inferred.
    /// </summary>
    /// <remarks>
    /// A <c>data</c> directive is recovered, because the query a component imports is a name the model already knows
    /// and can be checked against. Everything else a screen declares - <c>title</c>, <c>section</c>, <c>table</c> and
    /// <c>summary</c> with their columns and fields, <c>action</c> and <c>navigate to</c> - is structure expressed in
    /// JSX and component properties, which this generator does not read. A guessed column is worse than an absent
    /// one, so none of it is inferred and every screen says so.
    /// </remarks>
    public const string ScreenStructureNotInferred = "SP0028";

    // SP0029 is deliberately unused. It was assigned to a code that was retired before the first release, and the
    // sequence is left with the gap rather than closed up: a code is what a consumer suppresses and groups on, so
    // handing this number to something else would silently change what an existing suppression means. Nothing is to
    // be declared with it.

    /// <summary>
    /// A type is referred to by a name the document never declares, because Screenplay cannot express it.
    /// </summary>
    /// <remarks>
    /// A Screenplay type reference is a single identifier. A constructed generic loses its arguments the moment it is
    /// written as one - a map of names to values becomes the word <c>KeyValuePair</c> - and the document then refers
    /// to a type it never declares. Nothing better can be written, so what was lost is said instead.
    /// <para>
    /// An interface or an abstract class ends in the same place by the opposite route. Its name survives being written
    /// as a single identifier, so nothing is lost in the writing; what cannot be written is the declaration, because a
    /// <c>type</c> says what a value holds and a contract leaves that to whatever implements it. Either way the
    /// property names something the document never introduces, which is the one thing worth saying about both.
    /// </para>
    /// </remarks>
    public const string UnmappableTypeReference = "SP0030";

    /// <summary>
    /// Two types share the simple name a concept is declared under, so only the first of them is described.
    /// </summary>
    public const string AmbiguousConceptName = "SP0031";

    /// <summary>
    /// A name is one the block it is written in reads as a directive of its own, so the line carrying it is left out.
    /// </summary>
    /// <remarks>
    /// Screenplay is line based and every block decides what a line is from its first word. A command property called
    /// <c>Description</c> is written as <c>description RequestDescription</c>, which the command body reads as the
    /// description of the command and rejects; an event property called <c>Tag</c> is written as <c>tag Something</c>,
    /// which the event body reads as a tag and quietly swallows. Screenplay's keyword escape preserves the name as
    /// <c>@tag</c>, so this is reported as information that the generated name was escaped.
    /// </remarks>
    public const string NameReservedByGrammar = "SP0032";

    /// <summary>
    /// A constant of an enumeration carries a value that enumeration declares no member with.
    /// </summary>
    /// <remarks>
    /// A member of an enumeration is written as the name the concept declares it under - <c>"clientContact"</c> -
    /// which is recovered from the number the compiler hands over. A number nothing is declared with is a cast or
    /// several flags combined into one, and naming it would describe a value the application does not have, so the
    /// number is written as it stands and what it names is left unsaid.
    /// </remarks>
    public const string UnnamedEnumerationValue = "SP0033";

    /// <summary>
    /// The document that was generated does not compile, which is the generator being wrong rather than the source.
    /// </summary>
    /// <remarks>
    /// Every other code here names something the application declared that the language has no counterpart for,
    /// which is a gap the reader can see and work around. This one names a defect in the generator - a document the
    /// Screenplay compiler rejects is output nobody can use, and no way of writing an application avoids it. It
    /// exists because such a document is only ever found by reading every generated one back: a property named
    /// after a directive shipped once precisely because no fixture happened to use that name. The text is returned
    /// as it stands so the line that was rejected can be read.
    /// </remarks>
    public const string DocumentDidNotCompile = "SP0034";

    /// <summary>
    /// A value an artifact carries is a record no <c>type</c> declaration could be written for.
    /// </summary>
    /// <remarks>
    /// A record carrying several values is what a <c>type</c> declares, so an event property written as
    /// <c>days ApprovedDayLine[]</c> names a shape the document introduces and the line resolves. What is left is the
    /// handful of records no declaration can be written for: one carrying no value at all, and one whose simple name a
    /// concept or another record already holds - a document is one namespace of declarations, and the second of two
    /// names cannot be written twice. The concepts inside such a record are still recovered and declared, because a
    /// concept can be declared wherever it was reached from; the shape is not, so the property naming it refers to
    /// something the document never introduces. That is what this reports, and why it says which of the reasons it
    /// was: only one of them is closed by renaming something.
    /// </remarks>
    public const string UndeclarableShape = "SP0035";

    /// <summary>
    /// A screen reads through a query a different slice of the application declares.
    /// </summary>
    /// <remarks>
    /// A screen aggregating several read models is what an Event Modeling screen routinely is, and an import naming a
    /// query the model really holds is a binding rather than the noise every other unmatched import is. A <c>data</c>
    /// directive names a query by the bare name its slice declares it under, though, and an application declares
    /// <c>All</c> once per read model, so writing one down would say which query only by accident
    /// (Cratis/Screenplay#28). Naming the screen, the query and the slice declaring it is what a reader needs to see
    /// the binding the document is missing, and what turns it into one the moment a reference can carry the slice.
    /// </remarks>
    public const string CrossSliceQueryBinding = "SP0036";

    /// <summary>
    /// Two projects of one application declare an artifact of the same name in the same slice.
    /// </summary>
    /// <remarks>
    /// A slice is recovered from a namespace, and a namespace an application declares from two projects is one slice
    /// written in two places - the contracts of a bounded context sitting beside the handlers acting on them is the
    /// ordinary case. Everything both projects contribute belongs to that one slice, and everything within a slice is
    /// named once: a document declaring two commands called <c>PlaceOrder</c> in one slice says the same word twice
    /// and means it differently. The first is kept, because the projects are read in assembly name order and no other
    /// order is available to prefer by, and the second is reported rather than dropped where nobody would see it.
    /// </remarks>
    public const string RepeatedDeclarationAcrossProjects = "SP0037";

    /// <summary>
    /// The projects of an application are written in directories that share none above the root of the file system.
    /// </summary>
    /// <remarks>
    /// Every path in a document is written relative to a directory, because a document carrying the absolute layout of
    /// the machine that generated it is one nobody can commit or diff. With several projects that directory is the one
    /// they are all written under - a <c>Source</c> folder holding a project each - and a path relative to it says
    /// which project a file belongs to as well as where it sits within that project.
    /// <para>
    /// Projects checked out beside each other in unrelated places share nothing but the root of the file system, which
    /// is not a directory to write anything relative to. Each project's paths are therefore written relative to its own
    /// root, which keeps every one of them relative and still says where a file sits within its project - and says
    /// nothing about which project that is, so two files can come out as the same path. That is what this reports.
    /// </para>
    /// </remarks>
    public const string ProjectsWithoutASharedRoot = "SP0038";

    /// <summary>
    /// A scenario a slice is specified by states a step that could not be read, so the whole scenario was left out.
    /// </summary>
    /// <remarks>
    /// A specification is one concrete example, and an example missing the command it issues, the state it started
    /// from or the outcome it expects is not that example - it is a different one nobody wrote. So unlike a mapping,
    /// which stands on its own and can be left out while the rest of the block still says something true, a step that
    /// cannot be read takes the scenario with it. What made it unreadable is said, because the difference between a
    /// scenario resting on a value computed at run time and one resting on a helper the reader could inline is the
    /// difference between a gap that will always be there and one an afternoon closes.
    /// <para>
    /// A constant the document has no form for - a number cast to an enumeration that declares no member with that
    /// value, <see langword="default"/> for an enumeration declaring no zero member, several flags combined into a value no member
    /// is declared with (the document names one member and cannot state a combination), or <c>null</c> for a required
    /// record, list or enumeration - is such a step too. Scenarios proving a
    /// rejection routinely issue a command with exactly such a value, and writing the command without it would state a
    /// different command whose rejection has lost its cause, so the scenario is left out and this is what says which
    /// property, which value and why.
    /// </para>
    /// <para>
    /// Reported as a warning during both analysis and emission, at the declaring specification type (or its slice
    /// when no source declaration is available). Emission can also withhold a scenario whose command or concrete
    /// event sources cannot be represented, or whose generated fixtures or response expectations were not recovered.
    /// </para>
    /// </remarks>
    public const string UnreadableSpecification = "SP0039";

    /// <summary>
    /// A value a scenario states is code rather than a constant, so the scenario states everything but that value.
    /// </summary>
    /// <remarks>
    /// A scenario is written in the host language, where a value is routinely worked out at run time rather than
    /// written down. Screenplay states values and has no way to name one, so such a value is left out and the rest of
    /// the scenario stands: which events had happened, which command was issued and what followed are all still
    /// exactly what the source says. This is reported per value rather than per scenario for the same reason a
    /// produces mapping is - a reader counting what a scenario states against what the source states otherwise has no
    /// way of knowing which of the two it is looking at.
    /// <para>
    /// The identity two steps agree on is the exception, and it is the reason reporting per value is bearable at all.
    /// A fresh identity has no value to state, so a document leaving one out says exactly as much as the source does
    /// and there is no difference to report; on a real application that is the majority of everything this code ever
    /// said. It is recognized rather than guessed at, and narrowly - what counts as one is in
    /// <c>GeneratedIdentities</c> - so an identity derived from something, which is a value the source really states,
    /// is still reported.
    /// </para>
    /// <para>
    /// This is only for a value that is not a constant. A constant the document cannot hold - a number no member of an
    /// enumeration is declared with, a combination of flags, or <c>null</c> for a required property - is not reported
    /// here, because leaving it out would leave a scenario that no longer says what was written: it is reported as
    /// <see cref="UnreadableSpecification"/> together with the scenario.
    /// </para>
    /// </remarks>
    public const string UnreadableSpecificationValue = "SP0040";

    /// <summary>
    /// The application says how an artifact is served, which a Screenplay does not describe.
    /// </summary>
    /// <remarks>
    /// A Screenplay says what an application is, never how a caller reaches it. Paging, sorting and a route template
    /// are all real declarations about the running application, and all three concern the request rather than the
    /// model - so there is nothing in the document they could be written into, and nothing to gain from inventing
    /// somewhere. What is worth having is the reader knowing the question was asked at all: a document silent about
    /// paging reads exactly like an application that does not page. The three share one code because they share one
    /// reason, and a reader who does not want to hear about how the application is served suppresses all of it at
    /// once.
    /// </remarks>
    public const string ServingConcernWithoutCounterpart = "SP0041";

    /// <summary>
    /// A read model declares something the document has nowhere to hold.
    /// </summary>
    /// <remarks>
    /// A <c>readmodel</c> declaration states the shape of a read model - its properties, a description and the file
    /// declaring it - and nothing else, so anything a read model carries beyond that has nowhere to go. Tags are the
    /// case that matters, and they matter because an event's tags <em>are</em> printed: a reader seeing tags
    /// throughout the events and none on the read models would reasonably conclude that the read models carry none. This is separate from
    /// <see cref="ServingConcernWithoutCounterpart"/> because the reason is different - the declaration says nothing
    /// about the request, it is a declaration the document does not make at all.
    /// </remarks>
    public const string ReadModelFeatureWithoutCounterpart = "SP0042";

    /// <summary>
    /// A slice is specified by a scenario of a kind the language has nowhere to put, so the whole of it is left out.
    /// </summary>
    /// <remarks>
    /// Command, read-model, and event-append scenarios have counterparts in the language, and so does a reactor
    /// scenario of a reactor stated as a declarative reaction, asserting only the events it returned. A reactor
    /// scenario asserting what a collaborator was asked to do, asserting a command the reactor invokes rather than the
    /// facts it records, or written against a reactor whose handlers are code still describes the inside of a slice,
    /// not a portable outcome. It is reported rather than silently passed over because it holds a scenario and
    /// specifies real slice behavior.
    /// </remarks>
    public const string ScenarioWithoutCounterpart = "SP0043";

    /// <summary>
    /// A command's event source or stream route was omitted.
    /// </summary>
    /// <remarks>
    /// Event source and stream syntax exists as authoring-only constructs. With the option disabled, an Information
    /// diagnostic explains how to enable it. With the option enabled, an unreadable route remains a Warning.
    /// Only the readable concurrency dimensions are emitted when the route is omitted, so a document silent about
    /// it is not read as a command that appends to the default stream.
    /// </remarks>
    public const string EventSourceNotRepresentable = "SP0044";

    /// <summary>
    /// A command names a stream its event source definition does not declare.
    /// </summary>
    public const string EventStreamNotDeclared = "SP0045";

    /// <summary>
    /// The stream id takes part in the concurrency scope an event source definition declares, which has no value to state.
    /// </summary>
    public const string EventStreamIdConcurrencyNotRepresentable = "SP0046";

    /// <summary>
    /// A reactor or reducer observes the events of an event source definition and stream, which the Screenplay language cannot declare yet.
    /// </summary>
    /// <remarks>
    /// The latest published Screenplay syntax gives a reaction trigger a named event and nothing to narrow it to an
    /// event source or stream, and the projection syntax has no such selector either. The observer is still emitted,
    /// but for every event of its type, so this is reported so a document silent about the filter is not read as an
    /// observer of the whole event log.
    /// </remarks>
    public const string ObserverEventSourceNotRepresentable = "SP0047";

    /// <summary>
    /// A reactor or reducer names a stream its event source definition does not declare.
    /// </summary>
    public const string ObserverEventStreamNotDeclared = "SP0048";

    /// <summary>
    /// A command has several identity candidates, so no identifier was selected.
    /// </summary>
    public const string AmbiguousCommandIdentifier = "SP0049";

    /// <summary>
    /// A command's identity cannot be read as a directly returned required scalar property.
    /// </summary>
    public const string UnreadableCommandIdentifier = "SP0050";

    /// <summary>
    /// Explicit or unprovable production routing prevents using the command identifier as a destination.
    /// </summary>
    public const string UnrepresentableProductionDestination = "SP0051";

    /// <summary>A generated value or response is unreadable or outside the admitted ESM v7 subset.</summary>
    public const string UnreadableCommandResponse = "SP0052";

    /// <summary>A returned operation cannot be represented reliably by the authoring grammar.</summary>
    public const string UnreadableCommandOperation = "SP0053";

    /// <summary>A source or stream route cannot be read without guessing.</summary>
    public const string UnreadableCommandRoute = "SP0054";

    /// <summary>Provisioning or a read dependency lives in code rather than a supported declarative shape.</summary>
    public const string UnreadableCommandProvisioning = "SP0055";

    /// <summary>
    /// The generated document compiled but contains an unexpected semantic binding error.
    /// </summary>
    public const string DocumentDidNotBind = "SP0056";

    /// <summary>
    /// A read model the document refers to is left undeclared, because no declaration could be written that says what
    /// the application holds.
    /// </summary>
    /// <remarks>
    /// A read model is declared once, in one slice, under its simple name, with every property typed by something the
    /// document declares. When two read models share a simple name, when the name is already taken by a concept or a
    /// type, when a property is typed by something the document cannot declare, or when no slice refers to it, any
    /// declaration written would describe a shape the application does not have. The read model is left out instead,
    /// so whatever builds or reads it names it exactly as it did before read models were declared at all.
    /// </remarks>
    public const string UndeclarableReadModel = "SP0057";

    /// <summary>
    /// A value carries only encryption or audit suppression, which cannot imply an operational secret.
    /// </summary>
    public const string PartialSecretMarking = "SP0058";

    /// <summary>
    /// Compliance annotations were omitted from a concept used as an identifier or route destination.
    /// </summary>
    public const string ProtectedIdentityAnnotation = "SP0059";

    /// <summary>
    /// A specification event route requires the authoring-only option, so its scenario was withheld.
    /// </summary>
    public const string SpecificationRouteNotRepresentable = "SP0060";
}
