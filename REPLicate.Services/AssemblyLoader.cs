using System.Reflection;

namespace REPLicate.Services;

public static class AssemblyLoader {
    public static void LoadAllReferences(IEnumerable<string> references) {
        foreach (var refname in references) {
            Console.WriteLine($"Loading reference { refname }");
            try {
                Assembly.LoadFrom(refname);
                Console.WriteLine($"Loaded reference {refname}");
            } catch (Exception e) {
                Console.WriteLine($"Error loading reference: { e }");
            }
        }
    }

    public static ICollection<string> GetLoadedNamespaces() {
        var namespaces = new List<string>();

        var nsEnumerable = AppDomain.CurrentDomain
          .GetAssemblies()
          .SelectMany(a => a.GetTypes().Select(t => t.Namespace))
          .Distinct()
          .OfType<string>();

        try {
            foreach(var n in nsEnumerable) {
                namespaces.Add(n);
            }
        } catch (Exception e) {
            Console.WriteLine($"Exception trying to process namespace: { e }");
        }

        return namespaces;
    }
}
