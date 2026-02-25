using VDS.RDF;
using VDS.RDF.Query.Datasets;
using System;

using VDS.RDF.Query;

using VDS.RDF.Parsing;


namespace VDS.RDF.Shacl
{
    internal class DatasetWrapperGraph : Graph
    {
        internal InMemoryDataset Dataset { get; }
        internal IRefNode GraphName { get; }

        internal DatasetWrapperGraph(InMemoryDataset dataset)
        {
            Dataset = dataset;
            addDefaultAsNamedGraph();
        }

        internal DatasetWrapperGraph(InMemoryDataset dataset, IRefNode graphName) : this(dataset)
        {
            IGraph g = dataset[graphName];
            this.Assert(g.Triples);
            SetAsDefaultGraph();
        }

        internal DatasetWrapperGraph(InMemoryDataset dataset, IGraph graph) : this(dataset)
        {
            this.Assert(graph.Triples);
            SetAsDefaultGraph();
        }

        private void addDefaultAsNamedGraph()
        {
            if (!Dataset.HasGraph((IRefNode)VocabularyDs.TargetGraphDefault))
            {
                IGraph defaultGraph = Dataset[(IRefNode)null];
                var defaultAsNamedGraph = new Graph(VocabularyDs.TargetGraphDefault.Uri);
                foreach (var triple in defaultGraph.Triples)
                {
                    defaultAsNamedGraph.Assert(triple);
                }
                Dataset.AddGraph(defaultAsNamedGraph);
            }
        }

        private void SetAsDefaultGraph()
        {
            Dataset.RemoveGraph((IRefNode)null);
            var newDefaultGraph = new Graph();
            newDefaultGraph.Assert(this.Triples); 
            Dataset.AddGraph(newDefaultGraph);
        }

        internal void ResetDefaultGraph()
        {
            var defaultAsNamedGraph = Dataset[(IRefNode)VocabularyDs.TargetGraphDefault];
            var defaultGraph = new Graph();
            defaultGraph.Assert(defaultAsNamedGraph.Triples);
            Dataset.RemoveGraph((IRefNode)null);
            Dataset.AddGraph(defaultGraph);
        }
    }
}