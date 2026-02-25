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
using VDS.RDF.Query.Datasets;
using VDS.RDF.Shacl;

namespace VDS.RDF.Shacl.GraphCombinationElements
{
    internal class MinusCombination : GraphCombination
    {
        [DebuggerStepThrough]
        internal MinusCombination(INode node, IGraph shapesGraph)
            : base(node, shapesGraph)
        {
        }

        internal override IGraph TargetCombined(InMemoryDataset dataDataset)
        {
            var argumentsList = Graph.GetTriplesWithSubjectPredicate(this,VocabularyDs.MinusCombination).Single().Object;

            if (!argumentsList.IsListRoot(Graph))
                throw new Exception();

            IEnumerable<INode> arguments = Arguments(argumentsList, dataDataset);

            IGraph targetCombined = new Graph();
            IGraph targetCombinedPos = GraphCombination.Parse(arguments.First(), Graph).TargetCombined(dataDataset);
            IGraph targetCombinedNeg = GraphCombination.Parse(arguments.Last(), Graph).TargetCombined(dataDataset);


            foreach (Triple t in targetCombinedPos.Triples)
            {
                if (!targetCombinedNeg.Triples.Contains(t))
                {
                    targetCombined.Assert(t);
                }
            }

            return targetCombined;
        }
    }
}