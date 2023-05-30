using REPLicate.Services;

var rspPath = System.Environment.GetEnvironmentVariable("REPL_RSP_FILE")!;
var libsPath = System.Environment.GetEnvironmentVariable("REPL_LIBS_PATH")!;

var lines = File.ReadLines(rspPath);

var referenceFilePaths = FileReader.ExtractReferenceFilePaths(lines);

Console.WriteLine("Loading references...");
AssemblyLoader.LoadAllReferences(referenceFilePaths);
Console.WriteLine("Done loading references");


var usingStatements = FileReader.ExtractUsingAndGlobalUsingStatements(lines, libsPath);

var globalUsingsFiles = Directory.EnumerateFiles(
    libsPath,
    "*.GlobalUsings.g.cs",
    SearchOption.AllDirectories
);
var globalUsingStatements = FileReader.ExtractUsingStatementsFromGlobalUsingsFiles(globalUsingsFiles);

var linesInNewFile = FileReader.ComposeReplResponseFile(
    lines,
    referenceFilePaths,
    usingStatements.Concat(globalUsingStatements)
);

File.WriteAllLines(rspPath, linesInNewFile);
