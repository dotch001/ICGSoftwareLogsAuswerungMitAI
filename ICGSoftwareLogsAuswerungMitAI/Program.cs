using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ICGSoftware.Library;



namespace ICGSoftware.LogAuswertung
{
    public class FilterErrAndAskAIClass
    {

        static public async Task Main()
        {
            //await Library.LogsAuswerten.FilterErrAndAskAIClass.FilterErrAndAskAI();
        }



        /*
        public static async Task FilterErrAndAskAI()
        {

            var config = new ConfigurationBuilder()
                .AddJsonFile("applicationSettings.json")
                .Build();

            var settings = config.GetSection("ApplicationSettings").Get<ApplicationSettingsClass>();

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

            string model;

            // Configuration of ApplicationSettings
            

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
                        ConsoleLogsAndInformation(settings.Inform, ((j + 1) + " fertig von " + amountOfFiles + " (Kein Error gefunden)"));
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
                                    ConsoleLogsAndInformation(settings.Inform, $"File size: {fileSize / 1024} KB of file {fileInfo.Name}");

                                    if (fileSize / 1024 >= settings.maxSizeInKB)
                                    {
                                        madeNewFilesCount++;
                                        outputFile = Path.Combine(outputFilePath + "_" + madeNewFilesCount + ".txt");
                                        outputFileOld = outputFile;
                                        ConsoleLogsAndInformation(settings.Inform, "Neue Datei erstellt: " + outputFile);
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
                    ConsoleLogsAndInformation(settings.Inform, (j + 1) + " fertig von " + amountOfFiles);
                }

                // Asks AI about the files in the output folder (for each file)
                if (settings.AskAI)
                {
                    for (int k = 0; k < Directory.GetFiles(outputFolder).Length; k++)
                    {
                        string response = await AskAndGetResponse(outputFolder, k, fileAsText, settings);
                        Console.WriteLine(response);
                    }
                }
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
            Console.WriteLine($"\n\n----------------------------------------------{PathToFile.Substring(PathToFile.IndexOf("ExtentionLogsFolder\\ExtentionLog") + 1)}----------------------------------------------\n\n");
            string model = settings.models[settings.chosenModel];
            string response = await AskQuestionAboutFile(settings.ApiKey, settings.Question, fileAsText, model);
            return response;
        }


        public static void ConsoleLogsAndInformation(bool inform, string theInformation)
        {
            if (inform)
            {
                Console.WriteLine(theInformation);
            }
        }

        //ask deepseek about a file
        static async Task<string> AskQuestionAboutFile(string apiKey, string question, string FileAsText, string model)
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
                Console.WriteLine($"Error: {response.StatusCode}");
                Console.WriteLine(await response.Content.ReadAsStringAsync());
                return "[API error]";
            }

            var responseString = await response.Content.ReadAsStringAsync();

            var json = JsonNode.Parse(responseString);
            var messageContent = json?["choices"]?[0]?["message"]?["content"]?.ToString();

            var cleanedContent = Regex.Replace(messageContent ?? "", @"\n{3,}", "\n\n").Trim();

            return string.IsNullOrWhiteSpace(cleanedContent) ? "Raw Response: " + responseString : cleanedContent;
        }



        //function to ask ChatGPT about a file
        /*
        public static async Task AskChatGPTAboutFile(string apiKey, string filePath, string question)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // Step 1: Upload the file
            var form = new MultipartFormDataContent();
            form.Add(new StreamContent(File.OpenRead(filePath)), "file", Path.GetFileName(filePath));
            form.Add(new StringContent("assistants"), "purpose");

            var uploadResponse = await client.PostAsync("https://api.openai.com/v1/files", form);
            var uploadResult = await uploadResponse.Content.ReadAsStringAsync();

            if (!uploadResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("File hochladen nicht geschafft:");
                Console.WriteLine(uploadResult);
                return;
            }

            using var uploadJson = JsonDocument.Parse(uploadResult);
            string fileId = uploadJson.RootElement.GetProperty("id").GetString();
            Console.WriteLine(" File hochgeladen. ID: " + fileId);

            // Step 2: Ask a question (basic GPT-4 prompt, not full file analysis)
            var requestBody = new
            {
                model = "gpt-3.5",
                messages = new[]
                {
                    new { role = "user", content = question + "\n(File ID: " + fileId + ")" }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var result = await response.Content.ReadAsStringAsync();

            Console.WriteLine("ChatGPT Antwort:");
            Console.WriteLine(result);
        }
        */
        
    }
}