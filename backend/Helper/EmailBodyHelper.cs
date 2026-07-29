namespace Marketify.Helper
{
    public static class EmailBodyHelper
    {
        public static string GenerateEmailBody(string template, Dictionary<string, string> templateModel)
        {
            var templatePath = ResolveTemplatePath(template);
            var body = File.ReadAllText(templatePath);

            foreach (var item in templateModel)
            {
                body = body.Replace(item.Key, item.Value);
            }

            return body;
        }

        private static string ResolveTemplatePath(string template)
        {
            var fileName = $"{template}.html";
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "TemplateEmails", fileName),
                Path.Combine(Directory.GetCurrentDirectory(), "TemplateEmails", fileName),
            };

            var templatePath = candidates.FirstOrDefault(File.Exists);
            if (templatePath is not null)
            {
                return templatePath;
            }

            throw new FileNotFoundException(
                $"The email template '{fileName}' could not be found in the application content directories.");
        }
    }
}
