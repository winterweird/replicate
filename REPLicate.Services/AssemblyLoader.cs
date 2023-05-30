using System.Reflection;

namespace REPLicate.Services;

public static class AssemblyLoader {
    public static ICollection<Assembly> LoadAllReferences(IEnumerable<string> references) {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
        foreach (var v in assemblies) {
            Console.WriteLine($"FOUND LOADED ASSEMBLY { v }");
        }
        foreach (var refname in references) {
            Console.WriteLine($"Loading reference { refname }");
            try {
                var asm = Assembly.LoadFrom(refname);
                assemblies.Add(asm);
                Console.WriteLine($"Loaded reference {refname}");
            } catch (Exception e) {
                Console.WriteLine($"Error loading reference: { e }");
            }
        }

        return assemblies;
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
