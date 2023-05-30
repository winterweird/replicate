using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

var ctxFile = System.Environment.GetEnvironmentVariable("REPL_SCRIPT_FILE");
var libsPath = System.Environment.GetEnvironmentVariable("REPL_LIBS_PATH")!;

var program = File.ReadAllText(ctxFile!);
var options = ScriptOptions.Default.WithReferences(
    typeof(Microsoft.AspNetCore.Builder.WebApplication).Assembly,
    typeof(System.Net.Http.Json.JsonContent).Assembly,
    typeof(Microsoft.Extensions.DependencyInjection.IMvcBuilder).Assembly
);

ScriptState<object> script = await CSharpScript.RunAsync("var args = new string[] {};", options);

// TODO: Refactor this logic
var globalUsingsFiles = Directory.EnumerateFiles(libsPath, "*.GlobalUsings.g.cs", SearchOption.AllDirectories);
foreach (var globalUsing in globalUsingsFiles) {
  Console.WriteLine($"Processing global using file { globalUsing }");
  foreach (var l in File.ReadLines(globalUsing)) {
    if (l.StartsWith("global using global::")) {
      var lStripped = l.Replace("global using global::", "");
      script = await script.ContinueWithAsync($"using { lStripped }");
    }
  }
  Console.WriteLine($"Done processing global using file { globalUsing }");
}

script = await script.ContinueWithAsync(program, options);
var processServices = @"
static string PrettyTypeName(Type t)
{
    if (t.IsArray)
    {
        return PrettyTypeName(t.GetElementType()!) + ""[]"";
    }

    if (t.IsGenericType)
    {
        var nm = t.FullName ?? t.Name;
        var sqbidx = nm.IndexOf(""["");
        var chk = sqbidx > -1 ? sqbidx : nm.Length;
        var genericPart = string.Format(
                ""{0}<{1}>"",
                nm.Substring(0, nm.LastIndexOf(""`"", chk, StringComparison.InvariantCulture)),
                string.Join("", "", t.GetGenericArguments().Select(PrettyTypeName)));

        if (t.Name.LastIndexOf(""`"") == -1) {
            genericPart += ""."" + t.Name;
        }
        return genericPart.Replace(""+"", ""."");
    }

    return t.Name.Replace(""+"", ""."");
}
foreach (var service in builder.Services) {
    if (service.ImplementationType is not null) {
        Console.WriteLine(PrettyTypeName(service));
    }
}
";
await script.ContinueWithAsync(processServices, options);

System.Environment.Exit(0);

static string PrettyTypeName(Type t)
{
    if (t.IsArray)
    {
        return PrettyTypeName(t.GetElementType()!) + "[]";
    }

    if (t.IsGenericType)
    {
        var nm = t.FullName ?? t.Name;
        var sqbidx = nm.IndexOf("[");
        var chk = sqbidx > -1 ? sqbidx : nm.Length;
        var genericPart = string.Format(
                "{0}<{1}>",
                nm.Substring(0, nm.LastIndexOf("`", chk, StringComparison.InvariantCulture)),
                string.Join(", ", t.GetGenericArguments().Select(PrettyTypeName)));

        if (t.Name.LastIndexOf("`") == -1) {
            genericPart += "." + t.Name;
        }
        return genericPart.Replace("+", ".");
    }

    return t.Name.Replace("+", ".");
}
