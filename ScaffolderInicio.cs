namespace Scaffolder
{
    public static class ScaffolderInicio
    {
        public static void GenerateFromEntityFile(string entityFilePath)
        {
            // Obtener el nombre de la entidad a partir del archivo .cs
            string entityName = Path.GetFileNameWithoutExtension(entityFilePath);

            // Generar los archivos a partir del archivo de entidad
            GenerateEntityClass(entityName, entityFilePath);
            GenerateEntityCommandsAndQueries(entityName);
            GenerateEntityMappings(entityName);
            GenerateEntityController(entityName);
        }

        private static void GenerateEntityClass(string entityName, string entityFilePath)
        {
            // Leer el contenido del archivo de entidad
            string entityClassContent = File.ReadAllText(entityFilePath);

            // Generar el archivo de clase de entidad
            string entityClassPath = Path.Combine("Entities", $"{entityName}.cs");
            File.WriteAllText(entityClassPath, entityClassContent);
            Console.WriteLine($"Generated {entityClassPath}");
        }

        private static void GenerateEntityCommandsAndQueries(string entityName)
        {
            // Generar archivos de comandos y consultas a partir de plantillas
            GenerateFileFromTemplate("CQRS/Commands/{0}Command.cs", entityName);
            GenerateFileFromTemplate("CQRS/Queries/{0}Query.cs", entityName);
        }

        private static void GenerateEntityMappings(string entityName)
        {
            // Generar archivo de asignaciones a partir de plantilla
            GenerateFileFromTemplate("Mappings/{0}Mappings.cs", entityName);
        }

        private static void GenerateEntityController(string entityName)
        {
            // Generar archivo de controlador a partir de plantilla
            GenerateFileFromTemplate("Controllers/{0}Controller.cs", entityName);
        }

        private static void GenerateFileFromTemplate(string templatePath, string entityName)
        {
            // Leer el contenido de la plantilla
            string templateFilePath = Path.Combine("Templates", string.Format(templatePath, entityName));
            string templateContent = File.ReadAllText(templateFilePath);

            // Generar el archivo final con el nombre de la entidad
            string generatedFilePath = Path.Combine(Path.GetDirectoryName(templateFilePath), string.Format(Path.GetFileName(templateFilePath), entityName));
            File.WriteAllText(generatedFilePath, templateContent);
            Console.WriteLine($"Generated {generatedFilePath}");
        }
    }
}
