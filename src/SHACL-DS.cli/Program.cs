using System;
using System.IO;
using System.Linq;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Shacl;
using VDS.RDF.Shacl.Validation;
using VDS.RDF.Writing;

using IOPath = System.IO.Path;
using SysStringWriter = System.IO.StringWriter;

internal static class Program
{
    // Usage:
    // dotnet run --project src/SHACL-DS.cli --data <path> --shapes <path> [--report <path>] [--output-format ttl|json] [--mode auto|shacl|shaclds]
    // Exit codes: 0=conforms, 1=nonconformant, 2=error/usage
    private static int Main(string[] args)
    {
        try
        {
            var options = Options.Parse(args);
            if (!options.IsValid)
            {
                PrintHelp();
                return 2;
            }

            var mode = ResolveMode(options);
            Report report;

            if (mode == Mode.Shacl)
            {
                var dataGraph = LoadDataGraph(options.DataPath!);
                var shapesGraph = LoadShapesGraph(options.ShapesPath!);
                var shapes = new ShapesGraph(shapesGraph);
                report = shapes.Validate(dataGraph);
            }
            else
            {
                // SHACL-DS mode
                var dataStore = LoadDataDataset(options.DataPath!);
                var shapesStore = LoadShapesDataset (options.ShapesPath!);

                // Warning: shapes dataset has no named graphs (default graph only)
                if (!shapesStore.Graphs.Any(g => g.BaseUri != null))
                {
                    Console.Error.WriteLine("[warning] SHACL-DS: shapes dataset has NO named graphs (e.g., loaded from .ttl/.nt).");
                    Console.Error.WriteLine("          In SHACL-DS, only named graphs actually validate data.");
                    Console.Error.WriteLine("          Result: validation blankly conforms. Use TriG/TriX with named graphs or --mode shacl.");
                }

                var dataDS = new InMemoryDataset(dataStore);
                var shapesDS = new ShapesDataset(shapesStore);
                report = shapesDS.Validate(dataDS);
            }

            if (!string.IsNullOrWhiteSpace(options.ReportPath))
            {
                SaveReport(report, options.ReportPath!, options.Format);
                Console.WriteLine($"{(report.Conforms ? "✔ Conforms" : "✘ Non-conformant")} — report: {IOPath.GetFullPath(options.ReportPath!)}");
            }
            else
            {
                WriteReportToStdout(report, options.Format);
            }

            return report.Conforms ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error:");
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
    }

    // -------- Mode & CLI parsing --------

    private enum Mode { Shacl, ShaclDs }

    private static Mode ResolveMode(Options options)
    {
        if (!string.IsNullOrWhiteSpace(options.ModeOverride))
        {
            return options.ModeOverride!.ToLowerInvariant() switch
            {
                "shacl" => Mode.Shacl,
                "shaclds" => Mode.ShaclDs,
                "auto" => ResolveAuto(options),
                _ => throw new ArgumentException($"Unknown mode: {options.ModeOverride}")
            };
        }
        return ResolveAuto(options);
    }

    //  - Data graph + Shapes graph      → SHACL
    //  - Data dataset + Shapes graph    → SHACL (Data dataset is unioned into a single data graph)
    //  - Data graph + Shapes dataset    → SHACL-DS (Data graph as default graph of a data dataset)
    //  - Data dataset + Shapes dataset  → SHACL-DS
    private static Mode ResolveAuto(Options options)
    {
        bool dataIsDs = IsDatasetPath(options.DataPath!);
        bool shapesIsDs = IsDatasetPath(options.ShapesPath!);

        if (shapesIsDs) return Mode.ShaclDs;
        if (dataIsDs) return Mode.Shacl;
        return Mode.Shacl;
    }

    private static bool IsDatasetPath(string path)
    {
        var ext = IOPath.GetExtension(path).ToLowerInvariant();
        return ext is ".trig" or ".trix" or ".nq" or ".nquads";
    }

    private sealed class Options
    {
        public string? DataPath { get; private set; }
        public string? ShapesPath { get; private set; }
        public string? ReportPath { get; private set; }
        public string Format { get; private set; } = "ttl";
        public string? ModeOverride { get; private set; } // auto|shacl|shaclds
        public bool IsValid => !string.IsNullOrWhiteSpace(DataPath) && !string.IsNullOrWhiteSpace(ShapesPath);

        public static Options Parse(string[] args)
        {
            var options = new Options();
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--data" when i + 1 < args.Length: options.DataPath = args[++i]; break;
                    case "--shapes" when i + 1 < args.Length: options.ShapesPath = args[++i]; break;
                    case "--report" when i + 1 < args.Length: options.ReportPath = args[++i]; break;
                    case "--output-format" when i + 1 < args.Length: options.Format = args[++i]; break;
                    case "--mode" when i + 1 < args.Length: options.ModeOverride = args[++i]; break;
                    case "-h":
                    case "--help": return new Options(); // force help
                }
            }
            return options;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("SHACL-DS CLI");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project src/SHACL-DS.cli --data <path> --shapes <path> [--report <path>] [--output-format ttl|json] [--mode auto|shacl|shaclds]");
        Console.WriteLine();
        Console.WriteLine("Auto mode (based on BOTH files):");
        Console.WriteLine("     - Data graph + Shapes graph      → SHACL");
        Console.WriteLine("     - Data dataset + Shapes graph    → SHACL (dataset is unioned into a single data graph)");
        Console.WriteLine("     - Data graph + Shapes dataset    → SHACL-DS (data graph as the default graph of a dataset)");
        Console.WriteLine("     - Data dataset + Shapes dataset  → SHACL-DS");
        Console.WriteLine();
        Console.WriteLine("When --mode is FORCED (overrides auto):");
        Console.WriteLine("     - SHACL: any DATASET input (.trig/.trix/.nq/.nquads) is UNION-MERGED into a single graph");
        Console.WriteLine("     - SHACL-DS: any GRAPH input (.ttl/.nt/.rdf/.owl/.n3) is treated as the DEFAULT GRAPH of a dataset");
        Console.WriteLine();
        Console.WriteLine("Exit codes: 0=conforms, 1=nonconformant, 2=error/usage");
    }

    // -------- Loaders (Graph vs Dataset) --------

    // SHACL

    private static IGraph LoadDataGraph(string path) => LoadGraphOrMergedDataset(path);
    private static IGraph LoadShapesGraph(string path) => LoadGraphOrMergedDataset(path);

    private static IGraph LoadGraphOrMergedDataset(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("File not found", path);

        var ext = IOPath.GetExtension(path).ToLowerInvariant();

        // Graph inputs → parse as a single graph
        if (ext is ".ttl" or ".nt" or ".rdf" or ".owl" or ".n3")
        {
            var graph = new Graph();
            switch (ext)
            {
                case ".ttl": new TurtleParser().Load(graph, path); break;
                case ".nt": new NTriplesParser().Load(graph, path); break;
                case ".rdf":
                case ".owl": new RdfXmlParser().Load(graph, path); break;
                case ".n3": new Notation3Parser().Load(graph, path); break;
            }
            return graph;
        }

        // Dataset inputs → load store then union-merge to a single graph
        if (ext is ".trig" or ".trix" or ".nq" or ".nquads")
        {
            var store = new TripleStore();
            switch (ext)
            {
                case ".trig": new TriGParser().Load(store, path); break;
                case ".trix": new TriXParser().Load(store, path); break;
                case ".nq":
                case ".nquads": new NQuadsParser().Load(store, path); break;
            }
            return MergeStoreToGraph(store);
        }

        throw new NotSupportedException($"Unsupported RDF/quad format: {ext}");
    }

    private static IGraph MergeStoreToGraph(TripleStore store)
    {
        var merged = new Graph();
        foreach (var graph in store.Graphs)
        {
            merged.Merge(graph, true);
        }
        return merged;
    }

    private static TripleStore LoadDataDataset(string path) => LoadDataset(path);
    private static TripleStore LoadShapesDataset(string path) => LoadDataset(path);

    private static TripleStore LoadDataset(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("File not found", path);

        var store = new TripleStore();
        var ext = IOPath.GetExtension(path).ToLowerInvariant();

        switch (ext)
        {
            case ".trig":
                new TriGParser().Load(store, path);
                break;

            case ".trix":
                new TriXParser().Load(store, path);
                break;

            case ".nq":
            case ".nquads":
                new NQuadsParser().Load(store, path);
                break;

            // Graph inputs as the default graph of a dataset
            case ".ttl":
                {
                    IGraph graph = new Graph();
                    new TurtleParser().Load(graph, path);
                    store.Add(graph);
                    break;
                }
            case ".nt":
                {
                    IGraph graph = new Graph();
                    new NTriplesParser().Load(graph, path);
                    store.Add(graph);
                    break;
                }

            default:
                throw new NotSupportedException($"Unsupported dataset format: {ext}");
        }

        return store;
    }

    // -------- Report output --------

    private static void WriteReport(Report report, string format, TextWriter writer)
    {
        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            var store = new TripleStore();
            store.Add(report.Graph);
            new JsonLdWriter().Save(store, writer);
        }
        else
        {
            // .ttl
            new CompressingTurtleWriter().Save(report.Graph, writer);
        }
    }

    private static void SaveReport(Report report, string path, string format)
    {
        Directory.CreateDirectory(IOPath.GetDirectoryName(IOPath.GetFullPath(path))!);
        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using var streamWriter = new StreamWriter(fileStream);
        WriteReport(report, format, streamWriter);
    }

    private static void WriteReportToStdout(Report report, string format)
    {
        WriteReport(report, format, Console.Out);
        Console.Out.Flush();
    }

}
