using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ProjectManager
{
    class ProjectFile
    {
        XDocument projectFile;

        ProjectFile(string targetFramework)
        {
            projectFile = new XDocument(  
                new XElement("Project",
                    new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                    new XElement("PropertyGroup",
                        new XElement("OutputType", "Library"),
                        new XElement("TargetFramework", targetFramework)),
                    new XElement("ItemGroup")));
        }

        static void Main(string[] args)
        {
            if (args.Length > 4)
            {
                if ((args[0] == "create") && (args[2] == "framework"))
                {
                    string projectPath = args[1];
                    string targetFramework = args[3];
                    string[] sources = args.Skip(4).ToArray();
                    var projectFile = new ProjectFile(targetFramework);
                    projectFile.AddSources(sources);
                    Directory.CreateDirectory(Path.GetDirectoryName(projectPath));
                    projectFile.Save(projectPath + ".csproj");
                    return;
                }
            }

            Console.WriteLine("Usage: ProjectManager create <project_path> framework <framework_version> <source> [<source> ...]");
        }

        private void Save(string projectPath)
        {
            projectFile.Save(projectPath);            
        }

        private void AddSources(string[] paths)
        {
            foreach (var path in paths)
            {
                AddSource(path);
            }
        }

        private void AddSource(string path)
        {
            var itemGroup = projectFile.Nodes()
                .OfType<XElement>()
                .DescendantNodes()
                .OfType<XElement>().First(xy => xy.Name.LocalName == "ItemGroup");
            var xelem = AddContent(path, projectFile);
            itemGroup.Add(xelem);
        }

        private XElement AddContent(string pathToAdd, XDocument doc) 
        { 
            XNamespace rootNamespace = doc.Root.Name.NamespaceName; 
            var xelem = new XElement(rootNamespace + "Compile"); 
            xelem.Add(new XAttribute("Include", pathToAdd)); 
            return xelem;
        }	
    }
}
