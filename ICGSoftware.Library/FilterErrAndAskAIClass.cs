using ICGSoftware.Library.Logging;
using ICGSoftware.Library.LogsAuswerten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ICGSoftware.Library.ErrorsKategorisierenUndZählen;
using Microsoft.Graph.Models;



namespace ICGSoftware.Library.LogsAuswerten
{
    public class FilterErrAndAskAIClass
    {
        public static async Task<ApplicationSettingsClass?> giveSettings() 
        {

            var config = new ConfigurationBuilder()
                    .AddJsonFile("applicationSettings_LogsAuswerten.json")
                    .Build();

            var settings = config.GetSection("ApplicationSettings").Get<ApplicationSettingsClass>();

            return settings;
        }
        public static async Task<string> FilterErrAndAskAI()
        {

            

            // Declaring variables
            int amountOfFiles;
            string[] fileNames;

            string outputFolder;
            string outputFile = "";
            string outputFileOld = outputFile;
            string outputFilePath;

            string inputPath;

            string endTermOld;
            string endTermNew;

            bool isBetween = false;

            bool found = false;

            int madeNewFilesCount = 0;

            var overwritePrevention = 0;

            string fileAsText = "";

            string allResponses = "";

            // Configuration of ApplicationSettings

            var config = new ConfigurationBuilder()
                .AddJsonFile("applicationSettings_LogsAuswerten.json")
                .Build();

            var settings = config.GetSection("ApplicationSettings").Get<ApplicationSettingsClass>();


            try 
            { 
                // Looping through all input folder paths
                for (int i = 0; i < settings.inputFolderPaths.Length; i++)
                {
                    // Getting input folder paths and files (for reading and naming)
                    amountOfFiles = Directory.GetFiles(settings.inputFolderPaths[i]).Length;
                    fileNames = Directory.GetFiles(settings.inputFolderPaths[i]);

                    // Making output folder
                    if (settings.outputFolderPath == null)
                    {
                        outputFolder = settings.inputFolderPaths[i] + "\\ExtentionLogsFolder";
                        while (Directory.Exists(outputFolder))
                        {
                            overwritePrevention++;
                            outputFolder = settings.inputFolderPaths[i] + "\\ExtentionLogsFolder" + overwritePrevention;
                        }
                        Directory.CreateDirectory(outputFolder);
                    }
                    else
                    {
                        outputFolder = settings.outputFolderPath + "\\ExtentionLogsFolder";
                        if (!Directory.Exists(outputFolder)) { Directory.CreateDirectory(outputFolder); }
                        else
                        {
                            while (Directory.Exists(outputFolder))
                            {
                                overwritePrevention++;
                                outputFolder = settings.outputFolderPath + "\\ExtentionLogsFolder" + overwritePrevention;
                            }
                            Directory.CreateDirectory(outputFolder);
                        }
                    }



                    // Defining endTermOld (when endTermOld != endTermNew make new folder for different days)
                    endTermOld = fileNames[0].Replace(settings.inputFolderPaths[i] + "\\TritomWeb.Api", "").Substring(0, 4);

                    // Declaring a list to store extracted lines
                    List<string> extractedLines = new List<string>();

                    // Looping through all files in the input current folder
                    for (int j = 0; j < amountOfFiles; j++)
                    {
                        // OutputFilePath is declared
                        outputFilePath = Path.Combine(outputFolder + "\\ExtentionLog" + fileNames[j].Replace(settings.inputFolderPaths[i] + "\\TritomWeb.Api", "").Substring(0, 8));
                        // OutputFile is changed to include the file name and the number of files made
                        outputFile = Path.Combine(outputFilePath + "_" + madeNewFilesCount + ".txt");

                        // OutputFileOld is used to check if a new file is needed
                        if (outputFile.Split("_")[0] + ".txt" != outputFileOld.Split("_")[0] + ".txt")
                        {
                            extractedLines.Clear();
                            madeNewFilesCount = 0;
                            outputFileOld = outputFile;
                        }

                        // Declaring endTermNew (for comparing with endTermOld)
                        endTermNew = fileNames[j].Replace(settings.inputFolderPaths[i] + "\\TritomWeb.Api", "").Substring(0, 4);

                        // Checking if new output file is needed
                        if (endTermOld != endTermNew)
                        {
                            outputFile = Path.Combine(outputFilePath + "_" + madeNewFilesCount + ".txt");
                            outputFileOld = outputFile;
                            endTermOld = endTermNew;
                        }

                        // InputPath of current file
                        inputPath = fileNames[j];

                        // Resetting the found bool (whether the startTerm has been found) for the current loop
                        found = false;

                        // Checking if the startTerm is in the file
                        using (StreamReader reader = new StreamReader(inputPath))
                        {
                            string? lineread;
                            while ((lineread = reader.ReadLine()) != null)
                            {
                                if (lineread.Contains(settings.startTerm, StringComparison.OrdinalIgnoreCase))
                                {
                                    found = true;
                                }
                            }
                        }

                        // If the startTerm is not found, continue to the next file
                        if (!found)
                        {
                            // Informs of no errors
                            ConsoleLogsAndInformation(settings.inform, ((j + 1) + " fertig von " + amountOfFiles + " (Kein Error gefunden)"));
                            continue;
                        }
                        // If the startTerm is found, continue with extracting lines
                        else
                        {
                            // Scanning each line in the file
                            foreach (string line in File.ReadLines(inputPath))
                            {
                                // Checking if the line contains the endTerm (if so extracts all lines and resets isBetween)
                                if (line.Contains(endTermOld) && isBetween)
                                {
                                    isBetween = false;
                                    File.WriteAllLines(outputFile, extractedLines);

                                    if (File.Exists(outputFile))
                                    {
                                        FileInfo fileInfo = new FileInfo(outputFile);
                                        long fileSize = fileInfo.Length;
                                        ConsoleLogsAndInformation(settings.inform, $"File size: {fileSize / 1024} KB of file {fileInfo.Name}");

                                        if (fileSize / 1024 >= settings.maxSizeInKB - 20)
                                        {
                                            madeNewFilesCount++;
                                            outputFile = Path.Combine(outputFilePath + "_" + madeNewFilesCount + ".txt");
                                            outputFileOld = outputFile;
                                            ConsoleLogsAndInformation(settings.inform, "Neue Datei erstellt: " + outputFile);
                                            extractedLines.Clear();
                                        }
                                    }
                                }

                                // Checking if the line contains the startTerm (if so sets isBetween to true)
                                if (line.Contains(settings.startTerm)) { isBetween = true; }

                                // Checking if the line contains the endTerm (if so adds the line to extracted lines list)
                                if (isBetween) { extractedLines.Add(line); }
                            }
                        }

                        // Informs about the progress
                        ConsoleLogsAndInformation(settings.inform, (j + 1) + " fertig von " + amountOfFiles);
                    }

                    // Asks AI about the files in the output folder (for each file)
                    if (settings.AskAI)
                    {
                        for (int k = 0; k < Directory.GetFiles(outputFolder).Length; k++)
                        {
                            string response = await AskAndGetResponse(outputFolder, k, fileAsText, settings);
                            allResponses = allResponses + $"<b><br /><br />----------------------------------------------{outputFolder}----------------------------------------------<br /><br /></b>" + response;
                            ConsoleLogsAndInformation(settings.inform, response);
                        }
                    }
                    await ErrorsKategorisierenUndZählenClass.ErrorsKategorisieren(outputFolder);
                }
                
                return allResponses;
            }
            catch (Exception ex)
            {
                LoggingClass.LogInformation($"Error: {ex.Message}");
                return "Error: " + ex.Message;
            }

            
        }


        public static async Task<string> AskAndGetResponse(string outputFolder, int k, string fileAsText, ApplicationSettingsClass settings)
        {
            string[] filesInOutput = Directory.GetFiles(outputFolder);
            string PathToFile = filesInOutput[k];

            using (StreamReader reader = new StreamReader(PathToFile))
            {
                fileAsText = reader.ReadToEnd();
            }

            await Task.Delay(1000);
            ConsoleLogsAndInformation(settings.inform, $"\n\n----------------------------------------------{PathToFile}----------------------------------------------\n\n");
            string model = settings.models[settings.chosenModel];
            string response = await AskQuestionAboutFile(settings.ApiKey, settings.Question, fileAsText, model, settings);
            return response;
        }


        public static void ConsoleLogsAndInformation(bool inform, string theInformation)
        {
            if (inform)
            {
                Console.WriteLine(theInformation);
            }
        }

        //ask AI about a file
        public static async Task<string> AskQuestionAboutFile(string apiKey, string question, string FileAsText, string model, ApplicationSettingsClass settings)
        {
                var apiUrl = "https://openrouter.ai/api/v1/chat/completions";

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var requestBody = new
                {
                    model = model,
                    messages = new[]
                    {
            new { role = "user", content = question + FileAsText }
            }
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                var response = await client.PostAsync(apiUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    ConsoleLogsAndInformation(settings.inform, $"Error: {response.StatusCode}");
                    ConsoleLogsAndInformation(settings.inform, await response.Content.ReadAsStringAsync());
                    return "[API error]";
                }

                var responseString = await response.Content.ReadAsStringAsync();

                var json = JsonNode.Parse(responseString);
                var messageContent = json?["choices"]?[0]?["message"]?["content"]?.ToString();

                var cleanedContent = Regex.Replace(messageContent ?? "", @"\n{3,}", "\n\n").Trim();

                return string.IsNullOrWhiteSpace(cleanedContent) ? "Raw Response: " + responseString : cleanedContent;
            
                
        }
    }
}
