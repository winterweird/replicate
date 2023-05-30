using System.Text.RegularExpressions;

namespace REPLicate.Services;

public static class FileReader {
    private static Regex REFERENCE_EXCLUSION_PATTERN_PREFIX = new Regex(@"^!(-r |--reference )");
    private static Regex REFERENCE_PATTERN_PREFIX = new Regex(@"^(-r |--reference )");

    private static Regex USING_EXCLUSION_PATTERN_PREFIX = new Regex(@"^!(-u |--using )");
    private static Regex USING_PATTERN_PREFIX = new Regex(@"^(-u |--using )");

    private static Regex GLOBAL_USING_STATEMENT = new Regex(@"^global using global::(.*);$");

    private static Regex USING_OR_REFERENCE_PATTERN = new Regex(@"^!?(-u |--using |-r |--reference )");

    private static Regex REGEX_MATCHER = new Regex(@"^/.*/$");
    private static Regex GLOB_MATCHER = new Regex(@"\.\*$");
    private static Regex GLOB_WILDCARD_MATCHER = new Regex(@"\*$");

    public static ICollection<string> ExtractUsingStatementsFromGlobalUsingsFiles(IEnumerable<string> files) {
        return files.SelectMany(ExtractGlobalUsingStatementsFromFile).Distinct().ToList();
    }

    public static IEnumerable<string> ExtractGlobalUsingStatementsFromFile(string file) {
        return File.ReadLines(file).Where(l => GLOBAL_USING_STATEMENT.IsMatch(l))
            .Select(l => GLOBAL_USING_STATEMENT.Replace(l, m => m.Groups[m.Groups.Count - 1].ToString()));
    }

    public static IEnumerable<string> ComposeReplResponseFile(IEnumerable<string> lines, IEnumerable<string> references, IEnumerable<string> usings) {
        return references.Select(r => $"-r { r }")
            .Concat(usings.Select(u => $"-u { u }"))
            .Concat(lines.Where(l => !USING_OR_REFERENCE_PATTERN.IsMatch(l)))
            .ToList();
    }

    public static ICollection<string> ExtractReferenceFilePaths(IEnumerable<string> lines) {
        var exclude = lines.Where(l => REFERENCE_EXCLUSION_PATTERN_PREFIX.IsMatch(l))
            .Select(l => REFERENCE_EXCLUSION_PATTERN_PREFIX.Replace(l, ""))
            .Select(ToRegexOrStringMatcher)
            .ToList();

        return lines.Where(l => REFERENCE_PATTERN_PREFIX.IsMatch(l))
            .Select(l => REFERENCE_PATTERN_PREFIX.Replace(l, ""))
            .Where(l => !exclude.Any(e => e(l)))
            .ToList();
    }

    public static ICollection<string> ExtractUsingStatements(IEnumerable<string> lines)
        => ExtractUsingStatements(lines, AssemblyLoader.GetLoadedNamespaces());

    public static ICollection<string> ExtractUsingStatements(IEnumerable<string> lines, IEnumerable<string> namespaces) {
        var exclude = lines.Where(l => USING_EXCLUSION_PATTERN_PREFIX.IsMatch(l))
            .Select(l => USING_EXCLUSION_PATTERN_PREFIX.Replace(l, ""))
            .Select(ToGlobOrStringMatcher)
            .ToList();
        return lines.Where(l => USING_PATTERN_PREFIX.IsMatch(l))
            .Select(l => USING_PATTERN_PREFIX.Replace(l, ""))
            .SelectMany(l => MatchingNamespaces(l, namespaces, exclude))
            .ToList();
    }

    private static IEnumerable<string> MatchingNamespaces(string ns, IEnumerable<string> namespaces, IEnumerable<Func<string, bool>> exclude) {
        if (ns.EndsWith(".*")) {
            var stem = GLOB_WILDCARD_MATCHER.Replace(ns, "");
            return namespaces.Where(n => n.StartsWith(stem) && !exclude.Any(e => e(n)));
        }

        return namespaces.Where(n => n == ns);
    }

    private static Func<string, bool> ToGlobOrStringMatcher(string str) {
        if (GLOB_MATCHER.IsMatch(str)) {
            var stem = GLOB_WILDCARD_MATCHER.Replace(str, "");
            return s => s.StartsWith(stem);
        }

        return s => s == str;
    }

    private static Func<string, bool> ToRegexOrStringMatcher(string str) {
        if (REGEX_MATCHER.IsMatch(str)) {
            var regex = new Regex(REGEX_MATCHER.Replace(str, ""));
            return s => regex.IsMatch(s);
        }
        else {
            return s => str == s;
        }
    }

}
