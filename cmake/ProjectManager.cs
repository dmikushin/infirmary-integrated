using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ProjectManager
{
    class ProjectFile
    {
        XDocument projectFile;

        ProjectFile(XDocument projectFile)
        {
            this.projectFile = projectFile;
        }

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

        static ProjectFile Load(string path)
        {
            return new ProjectFile(XDocument.Load(path));
        }

        static void Main(string[] args)
        {
            if (args.Length >= 4)
            {
                if ((args[0] == "create") && (args[2] == "framework"))
                {
                    string projectPath = args[1];
                    string targetFramework = args[3];
                    string[] sources = args.Skip(4).ToArray();
                    var projectFile = new ProjectFile(targetFramework);
                    if (sources.Length > 0)
                        projectFile.AddSources(sources);
                    Directory.CreateDirectory(Path.GetDirectoryName(projectPath));
                    projectFile.Save(projectPath + ".csproj");
                    return;
                }
                else if ((args[0] == "modify") && (args[2] == "dependency"))
                {
                    string projectPath = args[1];
                    string[] references = args.Skip(3).ToArray();
                    var projectFile = ProjectFile.Load(projectPath + ".csproj");
                    projectFile.AddReferences(references);
                    projectFile.Save(projectPath + ".csproj");
                    return;
                }
            }

            Console.WriteLine("Usage: ProjectManager create <project_path> framework <framework_version> [<source> ...]");
            Console.WriteLine("Usage: ProjectManager modify <project_path> dependency <reference> [<reference> ...]");
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
            AddItem("Compile", path);
        }

        private void AddReferences(string[] references)
        {
            foreach (var reference in references)
            {
                AddReference(reference);
            }
        }

        private void AddReference(string reference)
        {
            AddItem("Reference", reference);
        }

        private void AddItem(string type, string value)
        {
            var itemGroups = projectFile
                .Nodes()
                .OfType<XElement>()
                .DescendantNodes()
                .OfType<XElement>()
                .Where(x => x.Name.LocalName == "ItemGroup");

            XElement targetItemGroup = null;
            foreach (var itemGroup in itemGroups)
            {
                if (itemGroup.Elements(type).Any())
                {
                    targetItemGroup = itemGroup;
                    break;
                }
            }

            if (targetItemGroup == null)
            {
                foreach (var itemGroup in itemGroups)
                {
                    if (itemGroup.IsEmpty)
                    {
                        targetItemGroup = itemGroup;
                        break;
                    }
                }
            }

            XNamespace rootNamespace = projectFile.Root.Name.NamespaceName; 
            if (targetItemGroup == null)
            {
                targetItemGroup = new XElement(rootNamespace + "ItemGroup");
                projectFile.Element("Project").Add(targetItemGroup);
            }

            var xelem = new XElement(rootNamespace + type); 
            xelem.Add(new XAttribute("Include", value)); 
            targetItemGroup.Add(xelem);
        }
    }
}
