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
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VDS.RDF.Query;
using VDS.RDF.Shacl.Validation;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Parsing;

namespace VDS.RDF.Shacl
{
  /// <summary>
  /// Represents a SHACL-DS Shapes Dataset that acts as a SHACL-DS prototype processor, enabling validation of RDF datasets.
  /// </summary>    
  public class ShapesDataset : InMemoryDataset
  {
    private readonly bool _isValidator;

    // Special private constructor to initialize a Shapes Dataset Shapes Dataset instance
    private ShapesDataset(TripleStore shapesDataset, bool isValidator)
        : base(shapesDataset, false)
    {
      _isValidator = isValidator;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShapesDataset"/> class.
    /// </summary>
    /// <param name="shapesDataset">The triple store containing the original SHACL-DS Shapes Dataset.</param>
    [DebuggerStepThrough]
    public ShapesDataset(TripleStore shapesDataset) : base(shapesDataset, false) { }

    /// <summary>
    /// Checks if the current Shapes Dataset is valid by validating it against the SHACL-DS Shapes Dataset.
    /// </summary>
    /// <returns>True if the Shapes Dataset is valid, otherwise false.</returns>
    public bool IsValid()
    {
      if (_isValidator)
      {
        return true;
      }

      var shapesDatasetShapesDatasetTripleStore = new TripleStore();
      var parser = new TriGParser();

      using (var stream = typeof(ShapesDataset).Assembly.GetManifestResourceStream("VDS.RDF.Shacl.shapesDatasetShapesDataset.trig"))
      {
        if (stream == null)
        {
          throw new InvalidOperationException("'shapesDatasetShapesDataset.trig' not found.");
        }
        using (var reader = new StreamReader(stream))
        {
          parser.Load(shapesDatasetShapesDatasetTripleStore, reader);
        }
      }

      var shapesDatasetShapesDataset = new ShapesDataset(shapesDatasetShapesDatasetTripleStore, true);
      var report = shapesDatasetShapesDataset.Validate(this);
      return report.Conforms;
    }


    /// <summary>
    /// Retrieves shape graphs and the graph name of their target graphs.
    /// </summary>
    /// <param name="dataDataset">The data dataset to be validated.</param>
    /// <returns>An enumeration of shape graphs and their corresponding target graphs in the dataset.</returns>
    public IEnumerable<(INode ShapesGraph, INode TargetDataGraph)> GetTargetGraphs(InMemoryDataset dataDataset)
    {
      var namedGraphs = string.Join(" ", dataDataset.GraphNames.Where(node => node != null).Select(graphName => "<" + graphName + ">"));
      var query = new SparqlParameterizedString(string.Format(@"
PREFIX shds: <https://w3id.org/shacl-ds#>
SELECT DISTINCT ?shapesGraph ?targetDataGraph
WHERE {{
  {{
    SELECT DISTINCT ?shapesGraph ?targetDataGraph {{
      VALUES ?all {{shds:default {0}}}
      VALUES ?named {{{0}}}
      {{
        SELECT * {{
          ?shapesGraph shds:targetGraph ?target .
        }}
      }}
      UNION {{
        SELECT * {{
          GRAPH ?shapesGraph {{
            ?shapesGraph shds:targetGraph ?target .
          }}
        }}
      }}
      UNION {{
        SELECT * {{
          ?shapesGraph shds:targetGraphPattern ?pattern .
          VALUES ?target {{{0}}}
          FILTER(REGEX(STR(?target), ?pattern))
        }}
      }}
      UNION {{
        SELECT * {{
          GRAPH ?shapesGraph {{
            ?shapesGraph shds:targetGraphPattern ?pattern .
            VALUES ?target {{{0}}}
            FILTER(REGEX(STR(?target), ?pattern))
          }}
        }}
      }}
      BIND(IF(?target = shds:all, ?all, IF(?target = shds:named, ?named, ?target)) AS ?targetDataGraph)
    }}
  }}
  MINUS {{
    SELECT DISTINCT ?shapesGraph ?targetDataGraph {{
      VALUES ?all {{shds:default {0}}}
      VALUES ?named {{{0}}}
      {{
        SELECT * {{
          ?shapesGraph shds:targetGraphExclude ?target .
        }}
      }}
      UNION {{
        SELECT * {{
          GRAPH ?shapesGraph {{
            ?shapesGraph shds:targetGraphExclude ?target .
          }}
        }}
      }}
      UNION {{
        SELECT * {{
          ?shapesGraph shds:targetGraphExcludePattern ?pattern .
          VALUES ?target {{{0}}}
          FILTER(REGEX(STR(?target), ?pattern))
        }}
      }}
      UNION {{
        SELECT * {{
          GRAPH ?shapesGraph {{
            ?shapesGraph shds:targetGraphExcludePattern ?pattern .
            VALUES ?target {{{0}}}
            FILTER(REGEX(STR(?target), ?pattern))
          }}
        }}
      }}
      BIND(IF(?target = shds:all, ?all, IF(?target = shds:named, ?named, ?target)) AS ?targetDataGraph)
    }}
  }}
}}
", namedGraphs));
      var result = (SparqlResultSet)(new LeviathanQueryProcessor(this)).ProcessQuery(new SparqlQueryParser().ParseFromString(query));

      foreach (var line in result)
      {
        if (line.HasValue("shapesGraph") && line.HasValue("targetDataGraph"))
        {
          yield return (line["shapesGraph"], line["targetDataGraph"]);
        }
      }
    }

    /// <summary>
    /// Retrieves shape graphs and their target graph combination (the combination is only parsed).
    /// </summary>
    /// <param name="dataDataset">The data dataset to be validated.</param>
    /// <returns>An enumeration of shape graphs and their combined target graphs.</returns>
    public IEnumerable<(INode ShapesGraph, GraphCombination TargetGraphCombination)> GetTargetGraphCombinations(InMemoryDataset dataDataset)
    {
      foreach (var triple in this.GetTriplesWithPredicate(VocabularyDs.TargetGraphCombination))
      {
        var targetGraphCombination = GraphCombination.Parse(triple.Object, this[(IRefNode)null]);
        yield return (triple.Subject, targetGraphCombination);
      }

      foreach (var graphName in this.GraphNames.Where(name => name != null))
      {
        foreach (var triple in this[(IRefNode)graphName].GetTriplesWithSubjectPredicate(graphName, VocabularyDs.TargetGraphCombination))
        {
          var targetGraphCombination = GraphCombination.Parse(triple.Object, this[(IRefNode)graphName]);
          yield return (triple.Subject, targetGraphCombination);
        }
      }
    }

    /// <summary>
    /// Checks the given data dataset against this shapes dataset for SHACL-DS conformance and reports validation results.
    /// </summary>
    /// <param name="dataDataset">The data dataset to be validated.</param>
    /// <returns>A SHACL-DS validation report containing possible validation results.</returns>
    public Report Validate(InMemoryDataset dataDataset)
    {
      if (!IsValid()) throw new InvalidOperationException("The Shapes Dataset is not a valid Shapes Dataset.");

      var report = Report.Create(new Graph());
      var resultCollection = new ResultCollection(report);

      // Target Graphs
      foreach (var (shapesGraphName, targetGraphName) in GetTargetGraphs(dataDataset))
      {
        var shapesGraph = new ShapesGraph(this[(IRefNode)shapesGraphName]);
        var targetGraph = new DatasetWrapperGraph(dataDataset, (IRefNode)targetGraphName);
        var singleReport = shapesGraph.Validate(targetGraph);

        foreach (var result in singleReport.Results)
        {
          result.SourceShapesGraph = shapesGraphName;
          result.FocusGraph = targetGraphName;
          resultCollection.AddFull(result);
        }
        targetGraph.ResetDefaultGraph();
      }

      // Target Graph Combinations
      foreach (var (shapesGraphName, targetGraphCombination) in GetTargetGraphCombinations(dataDataset))
      {
        var shapesGraph = new ShapesGraph(this[(IRefNode)shapesGraphName]);
        var targetGraph = new DatasetWrapperGraph(dataDataset, targetGraphCombination.TargetCombined(dataDataset));
        var singleReport = shapesGraph.Validate(targetGraph);

        foreach (var result in singleReport.Results)
        {
          result.SourceShapesGraph = shapesGraphName;
          result.FocusGraph = targetGraphCombination;
          resultCollection.AddFull(result, targetGraphCombination);
        }
        targetGraph.ResetDefaultGraph();
      }

      return report;
    }

    /// <summary>
    /// Checks whether the given data dataset conforms to this shapes dataset according to SHACL-DS.
    /// </summary>
    /// <param name="dataDataset">The data dataset to check for SHACL-DS conformance.</param>
    /// <returns>True if the data dataset conforms to the shapes dataset; otherwise, false.</returns>
    public bool Conforms(InMemoryDataset dataDataset)
    {
      return Validate(dataDataset).Conforms;
    }
  }
}
