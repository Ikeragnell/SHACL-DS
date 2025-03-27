using System;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Shacl;
using VDS.RDF.Shacl.Validation;

using VDS.RDF.Writing;


class Program
{
    static void Main()
    {
        string testCasesDir = "../test-cases";

        // Create a TriGParser for loading the .trig files
        TriGParser parser = new TriGParser();

        // Iterate through each test case directory
        foreach (string testCaseDir in Directory.GetDirectories(testCasesDir))
        {
            if (testCaseDir != "../test-cases\\SHACL-DS-0005-SPARQL-COMBINATION")
                continue;
            try
            {
                // Paths for data and shapes files
                string dataFile = System.IO.Path.Combine(testCaseDir, "data.trig");
                string shapesFile = System.IO.Path.Combine(testCaseDir, "shapes.trig");
                string reportFile = System.IO.Path.Combine(testCaseDir, "report.ttl");

                // Validate existence of required files
                if (!File.Exists(dataFile) || !File.Exists(shapesFile))
                {
                    Console.WriteLine($"Skipping test case: Missing data or shapes file in {testCaseDir}");
                    continue;
                }

                // Load the data and shapes
                TripleStore dataStore = new TripleStore();
                TripleStore shapeStore = new TripleStore();

                parser.Load(dataStore, dataFile);
                parser.Load(shapeStore, shapesFile);

                // Wrap the data and shapes as datasets
                InMemoryDataset dataDS = new InMemoryDataset(dataStore);
                ShapesDataset shapesDS = new ShapesDataset(shapeStore);

                // Perform validation
                var targetMap = shapesDS.GetTargetGraphs(dataDS).ToList();
                Report report = shapesDS.Validate(dataDS);

                // Save the validation report
                CompressingTurtleWriter writer = new CompressingTurtleWriter();
                writer.Save(report.Graph, reportFile);

                Console.WriteLine($"Report generated: {reportFile}");


            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}