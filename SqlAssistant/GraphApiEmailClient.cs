using Azure.Identity;
using Microsoft.Graph;

namespace SqlAssistant;

public class GraphApiEmailClient
{
    private ConfigOptions _configOptions = new();

    public GraphApiEmailClient() { }

    public GraphApiEmailClient(ConfigOptions options)
    {
        _configOptions = options;
    }

    public async Task SendAsync(List<string> to, string subject, string body, string fileName, byte[] attachmentContent, bool IsBodyHtml = false)
    {
        try
        {
            var scopes = new[] { _configOptions.Graph_Scope };

            var tenantId = _configOptions.Graph_TenantId;

            var clientId = _configOptions.Graph_ClientId;
            var clientSecret = _configOptions.Graph_ClientSecret;

            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            var clientSecretCredential = new ClientSecretCredential(tenantId, clientId.ToString(), clientSecret, options);

            var graphClient = new GraphServiceClient(clientSecretCredential, scopes);

            var attachments = new MessageAttachmentsCollectionPage();
            attachments.Add(new FileAttachment
            {
                ODataType = "#microsoft.graph.fileAttachment",
                Name = fileName,
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ContentBytes = attachmentContent
            });

            // Define e-mail message.
            var message = new Message
            {
                ToRecipients = to.ConvertAll(emailTo => new Recipient { EmailAddress = new EmailAddress() { Address = emailTo.Trim() } }),
                Subject = subject,
                Body = new ItemBody
                {
                    ContentType = IsBodyHtml ? BodyType.Html : BodyType.Text,
                    Content = body
                },
                Attachments = attachments
            };

            await graphClient
                    .Users[_configOptions.Graph_EmailFrom]
                    .SendMail(message)
                    .Request()
                    .PostAsync();
        }
        catch(Exception ex)
        {
            throw ex;
        }
    }
}
