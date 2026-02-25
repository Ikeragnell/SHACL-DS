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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VDS.RDF.Query;
using VDS.RDF.Query.Builder;
using VDS.RDF.Query.Patterns;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Shacl.GraphCombinationElements;

namespace VDS.RDF.Shacl
{
    /// <summary>
    /// Represents an abstract base class for SHACL-DS graph combinations.
    /// Provides mechanisms to parse and combine graphs based on SHACL-DS specification.
    /// </summary>
    public abstract class GraphCombination : GraphWrapperNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GraphCombination"/> class.
        /// </summary>
        /// <param name="node">The node representing this graph combination.</param>
        /// <param name="shapesGraph">The graph containing the SHACL-DS TargetGraphCombination declaration.</param>
        [DebuggerStepThrough]
        protected GraphCombination(INode node, IGraph shapesGraph)
            : base(node, shapesGraph)
        {
        }

        internal IEnumerable<INode> Arguments(INode argumentsList, InMemoryDataset dataDataset)
        {
            return NormalizedArgumentsList(Graph.GetListItems(argumentsList), dataDataset);
        }
        internal IEnumerable<INode> NormalizedArgumentsList(IEnumerable<INode> arguments, InMemoryDataset dataDataset)
        {
            foreach (var argument in arguments)
            {
                IEnumerable<INode> graphNames = dataDataset.GraphNames.Where(name => name != null);
                switch (argument)
                {
                    case INode t when t.Equals(VocabularyDs.TargetGraphAll):
                        {
                            foreach (var name in graphNames)
                            {
                                yield return name;
                            }
                            yield return VocabularyDs.TargetGraphDefault;
                            break;
                        }

                    case INode t when t.Equals(VocabularyDs.TargetGraphNamed):
                        {
                            foreach (var name in graphNames)
                            {
                                yield return name;
                            }
                            break;
                        }

                    default:
                        yield return argument;
                        break;
                }
            }
        }

        internal static GraphCombination Parse(INode value, IGraph graph)
        {
            if (value.NodeType == NodeType.Uri)
            {
                return new AtomCombination(value, graph);
            }

            INode predicate = graph.GetTriplesWithSubject(value).Single().Predicate;
            switch (predicate)
            {
                case INode t when t.Equals(VocabularyDs.OrCombination):
                    return new OrCombination(value, graph);

                case INode t when t.Equals(VocabularyDs.AndCombination):
                    return new AndCombination(value, graph);

                case INode t when t.Equals(VocabularyDs.MinusCombination):
                    return new MinusCombination(value, graph);

                default:
                    throw new Exception("Unknown graph combination type.");
            }
        }
        internal abstract IGraph TargetCombined(InMemoryDataset dataDataset);
    }
}
