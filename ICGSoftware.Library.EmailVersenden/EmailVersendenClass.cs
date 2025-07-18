﻿using Azure.Identity;
using ICGSoftware.Library.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
using Microsoft.Identity.Client;
using ICGSoftware.Library.LogsAuswerten;

namespace ICGSoftware.Library.EmailVersenden
{
    public class EmailVersendenClass
    {
        public static async Task Process(string Message)
        {
            try
            {
                var config = new ConfigurationBuilder()
                .AddJsonFile("applicationSettings_EmailVersenden.json")
                .Build();

                var settings = config.GetSection("ApplicationSettings").Get<ApplicationSettingsClass>();
                var authSettings = config.GetSection("AuthenticationSettings").Get<AuthenticationSettingsClass>();

                var confidentialClient = ConfidentialClientApplicationBuilder
                    .Create(authSettings.ClientId)
                    .WithClientSecret(authSettings.ClientSecret)
                    .WithAuthority($"https://login.microsoftonline.com/{authSettings.TenantId}")
                    .Build();

                var scopes = new[] { "https://graph.microsoft.com/.default" };
                var authResult = await confidentialClient.AcquireTokenForClient(scopes).ExecuteAsync();
                var accessToken = authResult.AccessToken;

                await SendEmail(settings, authSettings, Message);
                

            }
            catch (Exception ex)
            {
                LoggingClass.LoggerFunction("Error", $"Error sending email: {ex.Message}");
            }
        }

        public static async Task SendEmail(ApplicationSettingsClass settings, AuthenticationSettingsClass authSettings, string Message)
        {
            try
            {
                string outputFile = await FilterErrAndAskAIClass.giveOutputFilepath();

                var jsonContentBytes = await File.ReadAllBytesAsync(Path.Combine(outputFile, "Error Liste.txt"));
                var jsonFileName = Path.GetFileName(Path.Combine(outputFile, "Error Liste.txt"));

                var scopes = new[] { "https://graph.microsoft.com/.default" };
                var options = new ClientSecretCredentialOptions
                {
                    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
                };

                var clientSecretCredential = new ClientSecretCredential(
                    authSettings.TenantId,
                    authSettings.ClientId,
                    authSettings.ClientSecret,
                    options);

                var graphClient = new GraphServiceClient(clientSecretCredential, scopes);

                for (int i = 0; i < settings.recipientEmails.Length; i++)
                {
                    var attachment = new FileAttachment
                    {
                        OdataType = "#microsoft.graph.fileAttachment",
                        Name = jsonFileName,
                        ContentBytes = jsonContentBytes,
                        ContentType = "application/json"
                    };

                    var message = new Message
                    {
                        Subject = settings.subject,
                        Body = new ItemBody
                        {
                            ContentType = BodyType.Html,
                            Content = Message
                        },
                        ToRecipients = new List<Recipient>
            {
                new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = settings.recipientEmails[i]
                    }
                }
            },
                        Attachments = new List<Attachment> { attachment }
                    };

                    var sendMailBody = new SendMailPostRequestBody
                    {
                        Message = message,
                        SaveToSentItems = true
                    };

                    await graphClient.Users[settings.senderEmail]
                        .SendMail
                        .PostAsync(sendMailBody);
                }
                LoggingClass.LoggerFunction("Info", "Email sent successfully");
                return;
            }
            catch (Exception ex)
            {
                return;
            }
        }
    }
}
