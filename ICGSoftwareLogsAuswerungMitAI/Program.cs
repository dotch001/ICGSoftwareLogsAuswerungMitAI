using System;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;


namespace ICGSoftwareLogAuswertung
{
    class Program
    {
        static public async Task Main()
        {
            //declaring variables
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

            var OverwritePrevention = 0;

            //configuration of ApplicationSettings
            var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

            var settings = config.GetSection("ApplicationSettings").Get<ApplicationSettings>();


            //looping through all input folder paths
            for (int i = 0; i < settings.inputFolderPaths.Length; i++)
            {
                //getting input folder paths and files (for reading and naming)
                amountOfFiles = Directory.GetFiles(settings.inputFolderPaths[i]).Length;
                fileNames = Directory.GetFiles(settings.inputFolderPaths[i]);

                //making output folder
                outputFolder = settings.inputFolderPaths[i] + "\\Output";
                while (Directory.Exists(outputFolder)) { OverwritePrevention++; outputFolder = settings.inputFolderPaths[i] + "\\Output" + OverwritePrevention; }

                Directory.CreateDirectory(outputFolder);

                //defining endTermOld (when endTermOld != endTermNew make new folder for different days)
                endTermOld = fileNames[0].Replace(settings.inputFolderPaths[i] + "\\TritomWeb.Api", "").Substring(0, 4);

                //declaring a list to store extracted lines
                List<string> extractedLines = new List<string>();

                //looping through all files in the input current folder
                for (int j = 0; j < amountOfFiles; j++)
                {
                    outputFilePath = Path.Combine(outputFolder + "\\ExtentionLog" + fileNames[j].Replace(settings.inputFolderPaths[i] + "\\TritomWeb.Api", "").Substring(0, 8));

                    outputFile = Path.Combine(outputFilePath + "_" + madeNewFilesCount + ".txt");


                    if (outputFile.Split("_")[0] + ".txt" != outputFileOld.Split("_")[0] + ".txt")
                    {
                        extractedLines.Clear();
                        madeNewFilesCount = 0;
                        outputFileOld = outputFile;
                    }


                    //declaring endTermNew (for comparing with endTermOld)
                    endTermNew = fileNames[j].Replace(settings.inputFolderPaths[i] + "\\TritomWeb.Api", "").Substring(0, 4);

                    //checking if new output file is needed
                    if (endTermOld != endTermNew)
                    {
                        outputFile = Path.Combine(outputFilePath + "_" + madeNewFilesCount + ".txt");
                        outputFileOld = outputFile;
                        endTermOld = endTermNew;
                    }

                    //inputPath of current file
                    inputPath = fileNames[j];

                    //reseting the found bool (wether the startTerm has been found) for the current loop
                    found = false;

                    //checking if the startTerm is in the file
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
                    //if the startTerm is not found, continue to the next file
                    if (!found)
                    {
                        //informs of no errors
                        ConsoleLogsAndInformation(settings.inform, ((j + 1) + " fertig von " + amountOfFiles + " (Kein Error gefunden)"));
                        continue;
                    }
                    //if the startTerm is found, continue with extracting lines
                    else
                    {
                        //scanning each line in the file
                        foreach (string line in File.ReadLines(inputPath))
                        {
                            //checking if the line contains the endTerm (if so extracts all lines and resets isBetween)
                            if (line.Contains(endTermOld) && isBetween)
                            {

                                isBetween = false;
                                File.WriteAllLines(outputFile, extractedLines);

                                if (File.Exists(outputFile))
                                {
                                    FileInfo fileInfo = new FileInfo(outputFile);
                                    long fileSize = fileInfo.Length;
                                    ConsoleLogsAndInformation(settings.inform, $"File size: {fileSize / 1024} KB of file {fileInfo.Name}");

                                    if (fileSize / 1024 >= 300)
                                    {
                                        madeNewFilesCount++;
                                        outputFile = Path.Combine(outputFilePath + "_" + madeNewFilesCount + ".txt");
                                        outputFileOld = outputFile;
                                        ConsoleLogsAndInformation(settings.inform, "Neue Datei erstellt: " + outputFile);
                                        extractedLines.Clear();
                                    }
                                }

                            }

                            //checking if the line contains the startTerm (if so sets isBetween to true)
                            if (line.Contains(settings.startTerm)) { isBetween = true; }

                            //checking if the line contains the endTerm (if so adds the line to extracted lines list)     
                            if (isBetween) { extractedLines.Add(line); }

                        }
                    }

                    //informs about the progress
                    ConsoleLogsAndInformation(settings.inform, (j + 1) + " fertig von " + amountOfFiles);

                }

                //asks chatGPT about the files in the output folder (for each file)
                if (settings.AskGPT)
                {
                    for (int k = 0; k < Directory.GetFiles(outputFolder).Length; k++)
                    {
                        string[] filesInOutput = Directory.GetFiles(outputFolder);
                        string PathToFile = filesInOutput[k];
                        await AskChatGPTAboutFile(settings.ApiKey, PathToFile, settings.Question);
                    }
                }
                else { Console.WriteLine("done"); }
            }

        }

        public static void ConsoleLogsAndInformation(bool inform, string theInformation)
        {
            if (inform)
            {
                Console.WriteLine(theInformation);
            }
        }

        //function to ask ChatGPT about a file
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




    }
}