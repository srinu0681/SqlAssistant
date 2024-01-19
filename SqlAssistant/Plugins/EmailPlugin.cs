using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace SqlAssistant.Plugins;

public class EmailPlugin
{
    private ITurnContext<IMessageActivity> _turnContext;
    private ConfigOptions _configOptions;
    private GraphApiEmailClient _graphApiEmailClient;

    public EmailPlugin(ConfigOptions configOptions, GraphApiEmailClient graphApiEmailClient, ITurnContext<IMessageActivity> turnContext)
    {
        _turnContext = turnContext;
        _configOptions = configOptions;
        _graphApiEmailClient = graphApiEmailClient;
    }

    [KernelFunction, Description("Send the database results excel file as email. Use this function only if the goal is to email the database results.")]
    public async Task<string> Sendemail(
        [Description("The database results excel file full path")] string excelFilePath,
        [Description("The email id of the recipient")] string recipientEmailId)
    {
        await _turnContext.SendActivityAsync($"Sending email to {recipientEmailId}...");

        if (!System.IO.File.Exists(excelFilePath))
            return "Failed to send email as the excel file does not exist";

        if (String.IsNullOrWhiteSpace(recipientEmailId))
            return "Can not send email as the email recipient was not provided";

        var bytes = System.IO.File.ReadAllBytes(excelFilePath);

        try
        {
            await _graphApiEmailClient.SendAsync(new List<string> { recipientEmailId }, "Query Results", "Database results", Path.GetFileName(excelFilePath), bytes);
        }
        catch(Exception ex)
        {
            return $"Failed to send email as there is an error. Error: {ex.Message}";
        }

        return "Email sent successfully";
    }

    [KernelFunction, Description("Get the email id of the recipient. Use this function only if the email id is not provided.")]
    public async Task<string> GetEmailId([Description("The email recipient name or designation")]string recipient)
    {
        await _turnContext.SendActivityAsync($"Getting Email Id of {recipient}...");
        return _configOptions.Graph_EmailTo;
    }
}
