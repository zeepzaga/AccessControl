using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
var runtimeDir = @"C:\Program Files\dotnet\shared\Microsoft.NETCore.App\8.0.22";
foreach (var dll in System.IO.Directory.GetFiles(runtimeDir, "*.dll"))
{
    try { AssemblyLoadContext.Default.LoadFromAssemblyPath(dll); } catch {}
}
var pkgDir = @"C:\диплом\AccessControl\.nuget\packages";
foreach (var dll in System.IO.Directory.GetFiles(pkgDir, "*.dll", System.IO.SearchOption.AllDirectories).Where(p => p.Contains("faceaisharp") -or p.Contains("imagesharp") -or p.Contains("onnxruntime") -or p.Contains("communitytoolkit") -or p.Contains("mathnet") -or p.Contains("numsharp") -or p.Contains("simplesimd") -or p.Contains("microsoft.extensions")))
{
    try { AssemblyLoadContext.Default.LoadFromAssemblyPath(dll); } catch {}
}
var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(@"C:\диплом\AccessControl\.nuget\packages\faceaisharp.bundle\0.5.23\lib\net6.0\FaceAiSharp.Bundle.dll");
foreach (var t in asm.GetExportedTypes())
{
    Console.WriteLine(t.FullName);
    foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        Console.WriteLine("  " + m.ToString());
}
