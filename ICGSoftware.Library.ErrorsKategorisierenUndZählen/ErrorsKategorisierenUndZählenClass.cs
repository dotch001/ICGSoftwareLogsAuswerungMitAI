using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace ICGSoftware.Library.ErrorsKategorisierenUndZählen
{
    public class ErrorsKategorisierenUndZählenClass
    {
        static Dictionary<string, List<string>> categoryTimestamps = new Dictionary<string, List<string>>();
        static Dictionary<string, int> categoryCounts = new Dictionary<string, int>();

        public static async Task ErrorsKategorisieren(string outputFolder)
        {
            string[] fileNames = Directory.GetFiles(outputFolder);

            foreach (var file in fileNames)
            {
                foreach (string line in File.ReadLines(file))
                {
                    await GetError(line);
                }
            }

            await WriteToFile(outputFolder);
        }

        public static Task GetError(string line)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.Contains("[ERR]"))
                return Task.CompletedTask;

            var timestampMatch = Regex.Match(line, @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} [+\-]\d{2}:\d{2}");
            string timestamp = timestampMatch.Success ? timestampMatch.Value : "";

            string cleanedLine = Regex.Replace(line, "\".*?\"|'.*?'", "");

            int errIndex = cleanedLine.IndexOf("[ERR]");
            if (errIndex < 0)
                return Task.CompletedTask;

            string rawCategory = cleanedLine.Substring(errIndex);

            string noParams = Regex.Replace(rawCategory, @"\[Parameters=.*?\]", "[Parameters]");
            string normalized = Regex.Replace(noParams, @"\(\d+ms\)", "(ms)").Trim();

            int colonIndex = normalized.IndexOf(":");
            if (colonIndex > 0)
                normalized = normalized.Substring(0, colonIndex + 1);

            KategorisierenUndZählen(normalized, timestamp);

            return Task.CompletedTask;
        }


        public static void KategorisierenUndZählen(string category, string timestamp)
        {
            if (string.IsNullOrWhiteSpace(category))
                category = "(no category)";

            if (!categoryCounts.ContainsKey(category))
            {
                categoryCounts[category] = 0;
                categoryTimestamps[category] = new List<string>();
            }

            categoryCounts[category]++;
            categoryTimestamps[category].Add(timestamp);
        }

        private static async Task WriteToFile(string outputFolder)
        {
            string outputFile = Path.Combine(outputFolder, "Error Liste.txt");

            using StreamWriter writer = new StreamWriter(outputFile, false);

            foreach (var category in categoryCounts.Keys)
            {
                string timestamps = string.Join(", ", categoryTimestamps[category]);
                //await writer.WriteLineAsync($"{category} {categoryCounts[category]} mal auftreten um {timestamps}");
                var inner = new JObject
                {
                    ["Aufgetreten"] = categoryCounts[category] + " mal",
                    ["Timestamps"] = JToken.FromObject(categoryTimestamps[category])
                };


                var data = new JObject
                {
                    [category] = inner
                };

                string jsonString = JsonConvert.SerializeObject(data, Formatting.Indented);
                await writer.WriteLineAsync(jsonString);
            }
        }
    }
}
