using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EntityGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            string entityPath = "C:\\Users\\Juan Funes\\Documents\\GitHub\\scl.backend\\SCL.Domain\\Usuario.cs";
            string entityName = Path.GetFileNameWithoutExtension(entityPath);
            string templatePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Templates");
            string outputPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Temp");

            GenerateFiles(entityName, entityPath, templatePath, outputPath);
        }

        static void GenerateFiles(string entityName, string entityPath, string templatePath, string outputPath)
        {
            var properties = ExtractEntityPropertiesFromSourceFile(entityPath);

            var templates = Directory.GetFiles(templatePath, "*.*", SearchOption.AllDirectories);
            foreach (var template in templates)
            {
                var outputFileName = GetOutputFileName(template, entityName);
                var outputFilePath = Path.Combine(outputPath, outputFileName);
                var outputContent = GetOutputContent(template, entityName, properties);
                WriteFile(outputFilePath, outputContent);
                Console.WriteLine($"Archivo generado: {outputFileName}");
            }

            Console.WriteLine($"Archivos generados para la entidad '{entityName}'.");
        }

        public static List<EntityProperty> ExtractEntityPropertiesFromSourceFile(string entityPath)
        {
            var entityProperties = new List<EntityProperty>();

            try
            {
                var entityContent = File.ReadAllText(entityPath);

                // Obtener el tipo de la entidad utilizando Roslyn
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

        static TypeDeclarationSyntax GetEntityTypeFromSource(string entityContent)
        {
            var tree = CSharpSyntaxTree.ParseText(entityContent);
            var root = tree.GetRoot() as CompilationUnitSyntax;

            // Buscar la clase principal en el archivo
            var classNode = root?.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault();

            return classNode;
        }

        static string GetOutputFileName(string template, string entityName)
        {
            var fileName = Path.GetFileName(template);
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

        static string GetOutputContent(string template, string entityName, List<EntityProperty> properties)
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

            return content;
        }

        static void WriteFile(string outputPath, string content)
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
