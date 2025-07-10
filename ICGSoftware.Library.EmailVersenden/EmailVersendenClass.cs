using Azure.Identity;
using ICGSoftware.Library.EmailVersenden;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using Microsoft.Graph.Users.Item.SendMail;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ICGSoftware.Library.EmailVersenden
{
    public class EmailVersendenClass
    {
        public static async Task Process(string Message)
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

        public static async Task SendEmail(ApplicationSettingsClass settings, AuthenticationSettingsClass authSettings, string Message)
        {
            for (int i = 0; i < settings.recipientEmails.Length; i++)
            {

                var scopes = new[] { "https://graph.microsoft.com/.default" };

                var options = new ClientSecretCredentialOptions
                {
                    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
                };

                var clientSecretCredential = new ClientSecretCredential(
                    authSettings.TenantId, authSettings.ClientId, authSettings.ClientSecret, options);

                var graphClient = new GraphServiceClient(clientSecretCredential, scopes);


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
            Console.WriteLine("Email sent successfully!");
            
        }

    }
}
