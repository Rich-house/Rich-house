namespace Marketify.Helper
{
    public static class EmailBodyHelper
    {
        public static string GenerateEmailBody(string template, Dictionary<string, string> templateModel)
        {
            var temlatePath = $"{Directory.GetCurrentDirectory()}/TemplateEmails/{template}.html";
            var streamReader = new StreamReader(temlatePath);
            var body = streamReader.ReadToEnd();
            streamReader.Close();
            foreach (var item in templateModel)
            {
                body = body.Replace(item.Key, item.Value);
            }
            return body;
        }
    }
}
