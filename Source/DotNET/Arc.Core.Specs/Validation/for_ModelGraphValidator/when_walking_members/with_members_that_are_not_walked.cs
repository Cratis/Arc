// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation.for_ModelGraphValidator.when_walking_members;

/// <summary>
/// Only public instance properties that can be read without arguments are walked. A public property with a
/// non-public getter is still read; static, non-public, write-only and indexer properties, and fields, are not.
/// </summary>
public class with_members_that_are_not_walked : given.a_recording_model_graph_validator
{
    void Because() => Walk(new Model());

    [Fact] void should_walk_only_readable_public_instance_properties() =>
        Traversal.ShouldEqual("Model@ | String@visible | String@privatelyRead");

#pragma warning disable CA1822, CA1044, IDE0051, IDE0052, RCS1085, RCS1213, MA0041, CA1051, RCS1169, IDE0044, RCS1170, IDE0032
    class Model
    {
        public static string Static { get; set; } = "static";
        public string Field = "field";
        string _writeOnly = "write-only";
        public string Visible { get; set; } = "visible";
        public string WriteOnly { set => _writeOnly = value; }
        public string PrivatelyRead { private get; set; } = "privately read";
        internal string Internal { get; set; } = "internal";
        protected string Protected { get; set; } = "protected";
        string Private { get; set; } = "private";
        public string this[int index] => index.ToString();
    }
#pragma warning restore CA1822, CA1044, IDE0051, IDE0052, RCS1085, RCS1213, MA0041, CA1051, RCS1169, IDE0044, RCS1170, IDE0032
}
