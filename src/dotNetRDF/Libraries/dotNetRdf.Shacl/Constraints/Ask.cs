/*
// <copyright>
// dotNetRDF is free and open source software licensed under the MIT License
// -------------------------------------------------------------------------
//
// Copyright (c) 2025 CHIEM DAO, Davan, University of Liege
// Email: davan.chiemdao@uliege.be
// Copyright (c) 2009-2026 dotNetRDF Project (http://dotnetrdf.org/)
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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VDS.RDF.Nodes;
using VDS.RDF.Query;
using VDS.RDF.Query.Expressions.Primary;
using VDS.RDF.Query.Patterns;
using VDS.RDF.Shacl.Validation;

namespace VDS.RDF.Shacl.Constraints;

internal class Ask : Sparql
{
    [DebuggerStepThrough]
    internal Ask(Shape shape, INode value, IEnumerable<KeyValuePair<string, INode>> parameters)
        : base(shape, value, parameters)
    {
    }

    protected override string DefaultMessage => "SPARQL ASK must return true for all value nodes.";

    protected override string Query
    {
        get
        {
            try
            {
                return Vocabulary.Ask.ObjectsOf(this).Single().AsValuedNode().AsString();
            }
            catch (System.InvalidOperationException)
            {
                throw new ShaclProcessorException("A sh:SPARQLAskValidator must have exactly one sh:ask property.");
            }
        }
    }

        protected override bool ValidateInternal(IGraph dataGraph, INode focusNode, IEnumerable<INode> valueNodes, Report report, SparqlQuery query)
        {
            List<INode> invalidValues = new List<INode>();

            foreach (var valueNode in valueNodes)
            {
                SparqlQuery q = BindValue(query, valueNode);

                SparqlResultSet resultSet;
                if (dataGraph is DatasetWrapperGraph)
                {
                    var dataset = (dataGraph as DatasetWrapperGraph).Dataset;
                    ISparqlQueryProcessor processor = new LeviathanQueryProcessor(dataset);
                    resultSet = (SparqlResultSet)processor.ProcessQuery(q);
                }
                else
                {
                    resultSet = (SparqlResultSet)dataGraph.ExecuteQuery(q);
                }
                bool result = resultSet.Result;
                if (!result)
                {
                    invalidValues.Add(valueNode);
                }
            }

        return ReportValueNodes(focusNode, invalidValues, report);
    }

        private static SparqlQuery BindValue(SparqlQuery query, INode valueNode)
        {
            SparqlQuery q = query.Copy();

            var existingAssignments = q.RootGraphPattern.UnplacedAssignments.ToList();
            q.RootGraphPattern._unplacedAssignments = new List<IAssignmentPattern>();
            foreach (var existingAssignment in existingAssignments)
            {
                q.RootGraphPattern.TriplePatterns.Insert(0, new BindPattern(existingAssignment.VariableName, existingAssignment.AssignExpression));
            }

            q.RootGraphPattern.TriplePatterns.Insert(0, new BindPattern("value", new ConstantTerm(valueNode)));

        return q;
    }
}
