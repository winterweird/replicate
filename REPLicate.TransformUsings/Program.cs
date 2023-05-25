using System.Reflection;
using System.Text.RegularExpressions;

var rspPath = System.Environment.GetEnvironmentVariable("REPL_RSP_FILE")!;
var libsPath = System.Environment.GetEnvironmentVariable("REPL_LIBS_PATH")!;

Console.WriteLine($"Found REPL_RSP_FILE: ${ rspPath }");
Console.WriteLine($"Found REPL_LIBS_PATH: ${ libsPath }");

var lines = File.ReadLines(rspPath);

var referenceExclusionMatchers = new List<Func<string, bool>>();
var usingExclusionMatchers = new List<Func<string, bool>>();

foreach (var l in lines) {
  if (l.StartsWith("!-r ") || l.StartsWith("!--reference ")) {
    var str = l.Replace("!-r ", "").Replace("!--reference ", "");
    if (str.StartsWith("/") && str.EndsWith("/") && str.Length > 2) {
      // treat as regex
      str = str.Remove(0, 1).Remove(str.Length - 2, 1);
      var pattern = new Regex(str);
      referenceExclusionMatchers.Add(s => pattern.IsMatch(s));
    }
    else {
      // treat as str
      referenceExclusionMatchers.Add(s => s == str);
    }
  }
  else if (l.StartsWith("!-u ") || l.StartsWith("!--using ")) {
    var str = l.Replace("!-u ", "").Replace("!--using ", "");
    if (str.EndsWith(".*")) {
      str = str.Replace("*", "");
      usingExclusionMatchers.Add(s => s.StartsWith(str));
    }
    else {
      usingExclusionMatchers.Add(s => s == str);
    }
  }
}

List<String> linesInNewFile = new();

foreach (var l in lines) {
  if (l.StartsWith("-r ") || l.StartsWith("--reference ")) {
    var reference = l.Replace("-r ", "").Replace("--reference ", "");

    // Some references need to be excluded
    if (referenceExclusionMatchers.Any(isMatch => isMatch(reference))) {
      Console.WriteLine($"Skipping excluded reference {reference}");
      continue;
    }

    Console.WriteLine($"Loading reference {reference}");

    try {
      Assembly.LoadFrom(reference);
      Console.WriteLine($"Loaded reference {reference}");
    } catch (Exception e) {
      Console.WriteLine($"Error loading reference: { e }");
    }
  }
}

Console.WriteLine("Done loading references");

var nsEnumerable = AppDomain.CurrentDomain
  .GetAssemblies()
  .SelectMany(a => a.GetTypes().Select(t => t.Namespace))
  .Distinct()
  .OfType<string>();

var namespaces = new List<string>();

Console.WriteLine("Processing namespaces...");
try {
  foreach(var n in nsEnumerable) {
    namespaces.Add(n);
  }
} catch (Exception e) {
  Console.WriteLine($"Exception trying to process namespace: {e}");
}

Console.WriteLine("Done processing namespaces");

foreach (var l in lines) {
  if ((l.StartsWith("-u ") || l.StartsWith("--using ")) && l.EndsWith(".*")) {
    var usingOpt = l.Replace("-u ", "").Replace("--using ", "").Replace("*", "");
    Console.WriteLine($"Attempting to match namespaces using '{usingOpt}'");

    var nsMatching = namespaces
      .Where(n => n.StartsWith(usingOpt) && !usingExclusionMatchers.Any(isMatch => isMatch(n)))
      .Select(n => $"-u { n }");

    try {
      foreach (var nsm in nsMatching) {
        Console.WriteLine($"Matched '{usingOpt}' as '{nsm}'");
      }

      linesInNewFile.AddRange(nsMatching);
    } catch (Exception e) {
      Console.WriteLine($"Exception trying to process line { l }: {e}");
    }
  }
  else if (l.StartsWith("!")) {
    Console.WriteLine($"Skipping exclusion pattern: {l}");
  }
  else {
    Console.WriteLine($"Adding non-matching line {l}");
    linesInNewFile.Add(l);
  }
}

var globalUsingsFiles = Directory.EnumerateFiles(libsPath, "*.GlobalUsings.g.cs", SearchOption.AllDirectories);
foreach (var globalUsing in globalUsingsFiles) {
  Console.WriteLine($"Processing global using file { globalUsing }");
  foreach (var l in File.ReadLines(globalUsing)) {
    if (l.StartsWith("global using global::")) {
      var lStripped = l.Replace("global using global::", "").Replace(";", "");
      linesInNewFile.Add($"-u {lStripped}");
    }
  }
  Console.WriteLine($"Done processing global using file { globalUsing }");
}

File.WriteAllLines(rspPath, linesInNewFile);

System.Environment.Exit(0);
