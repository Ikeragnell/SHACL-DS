/*
// <copyright>
// dotNetRDF is free and open source software licensed under the MIT License
// -------------------------------------------------------------------------
//
// Copyright (c) 2025 CHIEM DAO, Davan, University of Liege
// Email: davan.chiemdao@uliege.be
// Copyright (c) 2009-2025 dotNetRDF Project (http://dotnetrdf.org/)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished
// to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
*/

namespace VDS.RDF.Shacl
{
    /// <summary>
    /// Represents the SHACL-DS vocabulary.
    /// </summary>
    public static class VocabularyDs
    {
        /// <summary>
        /// The SHACL-DS base URI.
        /// </summary>
        public const string BaseUri = "https://w3id.org/shacl-ds#";

        private static readonly NodeFactory Factory = new NodeFactory(new NodeFactoryOptions());

        /// <summary>
        /// Gets a node representing TargetGraph.
        /// </summary>
        public static IUriNode TargetGraph { get; } = ShaclDsNode("targetGraph");

        /// <summary>
        /// Gets a node representing TargetGraphExclude.
        /// </summary>
        public static IUriNode TargetGraphExclude { get; } = ShaclDsNode("targetGraphExclude");

        /// <summary>
        /// Gets a node representing TargetGraphCombination.
        /// </summary>
        public static IUriNode TargetGraphCombination { get; } = ShaclDsNode("targetGraphCombination");

        /// <summary>
        /// Gets a node representing OrCombination.
        /// </summary>
        public static IUriNode OrCombination { get; } = ShaclDsNode("or");

        /// <summary>
        /// Gets a node representing AndCombination.
        /// </summary>
        public static IUriNode AndCombination { get; } = ShaclDsNode("and");

        /// <summary>
        /// Gets a node representing MinusCombination.
        /// </summary>
        public static IUriNode MinusCombination { get; } = ShaclDsNode("minus");

        /// <summary>
        /// Gets a node representing SourceShapesGraph.
        /// </summary>
        public static IUriNode SourceShapesGraph { get; } = ShaclDsNode("sourceShapesGraph");

        /// <summary>
        /// Gets a node representing FocusGraph.
        /// </summary>
        public static IUriNode FocusGraph { get; } = ShaclDsNode("focusGraph");

        /// <summary>
        /// Gets a node representing TargetGraphAll.
        /// </summary>
        public static IUriNode TargetGraphAll { get; } = ShaclDsNode("all");

        /// <summary>
        /// Gets a node representing TargetGraphNamed.
        /// </summary>
        public static IUriNode TargetGraphNamed { get; } = ShaclDsNode("named");

        /// <summary>
        /// Gets a node representing TargetGraphDefault.
        /// </summary>
        public static IUriNode TargetGraphDefault { get; } = ShaclDsNode("default");

        /// <summary>
        /// Gets a node representing TargetGraphPattern.
        /// </summary>
        public static IUriNode TargetGraphPattern { get; } = ShaclDsNode("targetGraphPattern");

        /// <summary>
        /// Gets a node representing TargetGraphExcludePattern.
        /// </summary>
        public static IUriNode TargetGraphExcludePattern { get; } = ShaclDsNode("targetGraphExcludePattern");

        private static IUriNode ShaclDsNode(string name) => AnyNode($"{BaseUri}{name}");

        private static IUriNode AnyNode(string uri) => Factory.CreateUriNode(Factory.UriFactory.Create(uri));
    }
}
