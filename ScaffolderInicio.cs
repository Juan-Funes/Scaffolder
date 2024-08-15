using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Scaffolder
{
    public static class ScaffolderInicio
    {
        public static void GenerateFromEntityFile(string entityPath, string projectName)
        {
            string entityName = Path.GetFileNameWithoutExtension(entityPath);
            string templatePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Templates");

            GenerateFiles(entityName, entityPath, templatePath, projectName);
        }

        private static void GenerateFiles(string entityName, string entityPath, string templatePath, string projectName)
        {
            var properties = ExtractEntityPropertiesFromSourceFile(entityPath);

            var templates = Directory.GetFiles(templatePath, "*.*", SearchOption.AllDirectories);
            foreach (var template in templates)
            {
                var outputFileName = GetOutputFileName(template, entityName) + ".cs";
                var outputFilePath = GetOutputFilePath(entityPath, template, entityName, projectName);
                var outputContent = GetOutputContent(template, entityName, properties, projectName);
                WriteFile(outputFilePath, outputContent);
                Console.WriteLine($"Archivo generado: {outputFilePath}");
            }

            Console.WriteLine($"Archivos generados para la entidad '{entityName}'.");
        }

        private static string GetOutputFilePath(string entityPath, string template, string entityName, string projectName)
        {
            string folderName = GetOutputFolderName(template, entityName, projectName);
            string projectRootPath = GetProjectRootPath(entityPath); 

            string filePath = Path.Combine(projectRootPath, folderName, GetOutputFileName(template, entityName) + ".cs");

            return filePath;
        }
        private static string GetProjectRootPath(string entityPath)
        {
            string currentDirectory = Path.GetDirectoryName(entityPath);

            while (currentDirectory != null && !currentDirectory.EndsWith("Domain"))
            {
                currentDirectory = Directory.GetParent(currentDirectory).FullName;
            }

            if (currentDirectory != null)
            {
                currentDirectory = Directory.GetParent(currentDirectory).FullName;
            }
            return currentDirectory;
        }
        private static string GetOutputFolderName(string template, string entityName, string projectName)
        {
            if (template.Contains("Automapper"))
            {
                return Path.Combine($"{projectName}.Api", "Automapper", $"{entityName}Controller");
            }
            else if (template.Contains("Command"))
            {
                return Path.Combine($"{projectName}.Application", "Command", $"{entityName}Controller");
            }
            else if (template.Contains("Query"))
            {
                return Path.Combine($"{projectName}.Application", "Queries", $"{entityName}Controller");
            }
            else if (template.Contains("Controller"))
            {
                return Path.Combine($"{projectName}.Api", "Controllers");
            }
            else if (template.StartsWith("I") && template.Contains("Repository"))
            {
                return Path.Combine($"{projectName}.DataAccess.Interface");
            }
            else if (template.Contains("Repository"))
            {
                return Path.Combine($"{projectName}.DataAccess.EntityFramework");
            }
            else if (template.Contains("Interface"))
            {
                return Path.Combine($"{projectName}.Domain", "Interfaces", $"{entityName}Repository");
            }
            else if (template.EndsWith("ViewModel.txt"))
            {
                return Path.Combine($"{projectName}.Api", @"ViewModels", $"{entityName}Controller",FileName(template, entityName), "Input");
            }
            else if (template.EndsWith("ViewModelResponse.txt"))
            {
                return Path.Combine($"{projectName}.Api", @"ViewModels", $"{entityName}Controller", FileName(template, entityName), "Output");
            }
            else
            {
                return "";
            }
        }

        private static string FileName(string template, string entityName)
        {
            if (template.Contains("Create"))
            {
                return Path.Combine($"Create{entityName}");
            }
            else if (template.Contains("ByIdViewModel"))
            {
                return Path.Combine($"{entityName}ById");
            }
            else if (template.Contains("Delete"))
            {
                return Path.Combine($"Delete{entityName}");
            }
            else if (template.Contains("List"))
            {
                return Path.Combine($"{entityName}List");
            }
            else if (template.Contains("Update"))
            {
                return Path.Combine($"Update{entityName}");
            }
            else
            {
                return "";
            }
        }

        private static List<EntityProperty> ExtractEntityPropertiesFromSourceFile(string entityPath)
        {
            var entityProperties = new List<EntityProperty>();
            try
            {
                var entityContent = File.ReadAllText(entityPath);
                var entityType = GetEntityTypeFromSource(entityContent);

                if (entityType != null)
                {
                    foreach (var property in entityType.Members.OfType<PropertyDeclarationSyntax>())
                    {
                        string propertyName = property.Identifier.Text;
                        string propertyType = property.Type.ToString();

                        bool isKey = property.AttributeLists
                            .SelectMany(attrList => attrList.Attributes)
                            .Any(attr => attr.Name.ToString().Contains("Key"));

                        int? maxLength = null;
                        var maxLengthAttr = property.AttributeLists
                            .SelectMany(attrList => attrList.Attributes)
                            .FirstOrDefault(attr => attr.Name.ToString().Contains("MaxLength"));

                        if (maxLengthAttr?.ArgumentList?.Arguments.Count > 0)
                        {
                            maxLength = int.Parse(maxLengthAttr.ArgumentList.Arguments[0].ToString());
                        }

                        var entityProperty = new EntityProperty
                        {
                            Name = propertyName,
                            Type = propertyType,
                            IsKey = isKey,
                            MaxLength = maxLength
                        };

                        entityProperties.Add(entityProperty);
                    }
                }
                else
                {
                    Console.WriteLine($"No se pudo analizar la entidad en el archivo: {entityPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al extraer las propiedades de la entidad: {ex.Message}");
                throw;
            }
            return entityProperties;
        }

        private static TypeDeclarationSyntax GetEntityTypeFromSource(string entityContent)
        {
            var tree = CSharpSyntaxTree.ParseText(entityContent);
            var root = tree.GetRoot() as CompilationUnitSyntax;

            var classNode = root?.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault();

            return classNode;
        }

        private static string GetOutputFileName(string template, string entityName)
        {
            var fileName = Path.GetFileNameWithoutExtension(template);
            if (fileName.Contains("Entidad"))
            {
                fileName = fileName.Replace("Entidad", entityName);
            }
            if (fileName.Contains("entidad"))
            {
                fileName = fileName.Replace("entidad", entityName);
            }
            return fileName;
        }

        private static string GetOutputContent(string template, string entityName, List<EntityProperty> properties, string projectName)
        {
            var content = File.ReadAllText(template);
            content = Regex.Replace(content, @"Entidad", entityName);
            content = Regex.Replace(content, @"entidad", entityName);

            content = Regex.Replace(content, @"Proveedor", entityName);

            var propertyContent = new StringBuilder();
            foreach (var property in properties)
            {
                propertyContent.AppendLine($"        public {property.Type} {property.Name} {{ get; set; }}");
            }
            content = Regex.Replace(content, @"propiedades", propertyContent.ToString());

            if (template.Contains("Automapper"))
            {
                content = Regex.Replace(content, @"namespace.*", $"namespace {projectName}.Api.Automapper.{entityName}Controller");
            }
            else if (template.Contains("Command"))
            {
                content = Regex.Replace(content, @"namespace.*", $"namespace {projectName}.Application.Command.{entityName}Controller");
            }
            else if (template.Contains("Query"))
            {
                content = Regex.Replace(content, @"namespace.*", $"namespace {projectName}.Application.Queries.{entityName}Controller");
            }
            else if (template.Contains("Controller"))
            {
                content = Regex.Replace(content, @"namespace.*", $"namespace {projectName}.Api.Controllers");
            }
            else if (template.StartsWith("I") && template.Contains("Repository"))
            {
                content = Regex.Replace(content, @"namespace.*", $"namespace {projectName}.DataAccess.Interface.{entityName}Repository");
            }
            else if (template.Contains("Repository"))
            {
                content = Regex.Replace(content, @"namespace.*", $"namespace {projectName}.DataAccess.EntityFramework.{entityName}Repository");
            }
            else if (template.Contains("Interface"))
            {
                content = Regex.Replace(content, @"namespace.*", $"namespace {projectName}.Domain.Interfaces.{entityName}Repository");
            }
            else if (template.Contains("ServiceEvents"))
            {
                content = Regex.Replace(content, @"namespace.*", $"namespace {projectName}.Application.ServiceEvents.{entityName}ServiceEvent");
            }
            else if (template.Contains("ViewModel"))
            {
                content = Regex.Replace(content, @"namespace.*", $"namespace {projectName}.Api.ViewModels.{entityName}Controller");
            }

            content = ReplaceUsing(content, projectName);

            return content;
        }

        private static string ReplaceUsing(string content, string projectName)
        {
            content = Regex.Replace(content, @"projectName", projectName);
            return content;
        }
        private static void WriteFile(string outputPath, string content)
        {
            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            File.WriteAllText(outputPath, content);
        }
    }

    public class EntityProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsKey { get; set; }
        public int? MaxLength { get; set; }
    }
}