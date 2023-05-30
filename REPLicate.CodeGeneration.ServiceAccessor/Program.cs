using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.DependencyInjection;
using REPLicate.Services;

var rspFile = System.Environment.GetEnvironmentVariable("REPL_RSP_FILE");
var ctxFile = System.Environment.GetEnvironmentVariable("REPL_SCRIPT_FILE");
var libsPath = System.Environment.GetEnvironmentVariable("REPL_LIBS_PATH")!;

var program = File.ReadAllText(ctxFile!);

var lines = File.ReadAllLines(rspFile!);
var references = FileReader.ExtractReferenceFilePaths(lines);

var assemblies = AssemblyLoader.LoadAllReferences(references);
var usings = FileReader.ExtractUsingAndGlobalUsingStatements(lines, libsPath);

var options = ScriptOptions.Default.WithReferences(assemblies);

var usingStatements = usings.Select(u => $"using { u };");

// Initialize with using statements
ScriptState<object> script = await CSharpScript.RunAsync("", options);


foreach (var u in usingStatements) {
    try {
        script = await script.ContinueWithAsync(u, options);
    } catch (Exception) {
        // Console.WriteLine($"ERROR RUNNING {u}: {e}");
    }
}

// Create 'args' variable because it's not present in C# scripts for some
// reason.
script = await script.ContinueWithAsync("var args = new string[] {};", options);

// Run program
script = await script.ContinueWithAsync(program, options);

var servicesResult = await script.ContinueWithAsync($"return builder.Services;");
var services = (IServiceCollection)servicesResult.ReturnValue;

var REPLACE_TYPE_ARGS = new Regex(@"<.*>");

var workingTypes = new List<(string, string, string)>();

foreach (var s in services) {
    if (s.ImplementationType is null) {
        continue;
    }

    var stype = PrettyTypeName(s.ServiceType, true);
    var itype = PrettyTypeName(s.ImplementationType);
    var vname = REPLACE_TYPE_ARGS.Replace(itype, "").Split(".").Last();

    // Check if line works
    var testLine = $"{ stype } { vname } = app.Services.CreateScope().ServiceProvider.GetRequiredService<{ stype }>();";
    Console.WriteLine(testLine);
    bool works;
    try {
        await script.ContinueWithAsync(testLine, options);
        works = true;
    } catch (Exception e) {
        Console.WriteLine($"OOPS: {e}");
        works = false;
    }
    if (works) {
        Console.WriteLine($"{ stype } => { itype }");
        workingTypes.Add((stype, itype, vname));
    }
}

workingTypes = workingTypes.GroupBy(t => t.Item3)
    .Where(g => g.Count() == 1)
    .Select(g => g.First())
    .ToList();

program += @"
var svc = new AppServiceAccessor(app);
internal class AppServiceAccessor {
    private readonly IServiceProvider sp;
    internal AppServiceAccessor(WebApplication app) => sp = app.Services.CreateScope().ServiceProvider;
";

foreach (var t in workingTypes) {
    program += $"    internal { t.Item1 } { t.Item3 } => sp.GetRequiredService<{ t.Item1 }>();\n";
}

program += "}";

Console.WriteLine($"PROGRAM:\n{program}");

File.WriteAllText(ctxFile!, program);

static string PrettyTypeName(Type t, bool fullname = false) {
    var nm = t.FullName ?? t.Name;

    if (t.IsArray)
    {
        return PrettyTypeName(t.GetElementType()!) + "[]";
    }

    if (t.IsGenericType)
    {
        var sqbidx = nm.IndexOf("[");
        var chk = sqbidx > -1 ? sqbidx : nm.Length;
        var genericPart = string.Format(
                "{0}<{1}>",
                nm.Substring(0, nm.LastIndexOf("`", chk, StringComparison.InvariantCulture)),
                string.Join(", ", t.GetGenericArguments().Select(w => PrettyTypeName(w, fullname))));

        if (t.Name.LastIndexOf("`") == -1) {
            genericPart += "." + (fullname ? nm : t.Name);
        }
        return genericPart.Replace("+", ".");
    }

    return fullname ? nm.Replace("+", ".") : t.Name.Replace("+", ".");
}
