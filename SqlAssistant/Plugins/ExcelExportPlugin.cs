using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Data.SqlClient;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Data;
using System.Threading;

namespace SqlAssistant.Plugins;

public class ExcelExportPlugin
{
    private ExcelBuilder _excelBuilder;
    private ITurnContext<IMessageActivity> _turnContext;

    public ExcelExportPlugin(ITurnContext<IMessageActivity> turnContext, ExcelBuilder excelBuilder)
    {
        _turnContext = turnContext;
        _excelBuilder = excelBuilder;
    }

    [KernelFunction, Description("Run SQL against the AdventureWorksLT database. Use this function only if the final goal is to export the database results to excel.")]
    public async Task<string> RunQueryAndExportToExcel(
                    [Description("The query to run on SQL Server. When referencing tables, make sure to add the schema names.")] string query
    )
    {
        await _turnContext.SendActivityAsync($"Running query and exporting results to excel...");
        await _turnContext.SendActivityAsync(query);
        
        var filePath = _excelBuilder.ExportDatabaseResultsToExcelFile(query);

        IMessageActivity replyFileUploadConsent = MessageFactory.Attachment(CreateFileConsentAttachment(filePath, "Query Results"));
        await _turnContext.SendActivityAsync(replyFileUploadConsent);

        return "Exported to excel successfully";
    }

    [KernelFunction, Description("Run SQL against the AdventureWorksLT database and export the datasbe results to excel. Use this function only if the final goal is to send email.")]
    public async Task<string> RunQueryAndCreateExcel(
                    [Description("The query to run on SQL Server. When referencing tables, make sure to add the schema names.")] string query
    )
    {
        await _turnContext.SendActivityAsync($"Running query and exporting results to excel...");
        await _turnContext.SendActivityAsync(query);

        var filePath = _excelBuilder.ExportDatabaseResultsToExcelFile(query);

        return filePath;
    }

    private static Attachment CreateFileConsentAttachment(string filePath, string description)
    {
        FileInfo fi = new FileInfo(filePath);
        long? sizeInBytes = fi.Length;

        var fileName = Path.GetFileName(filePath);

        var consentContext = new Dictionary<string, string>
            {
                { "filename", filePath },
            };

        FileConsentCard card = new FileConsentCard()
        {
            AcceptContext = consentContext,
            DeclineContext = consentContext,
            SizeInBytes = sizeInBytes,
            Description = description
        };

        Attachment att = new Attachment(FileConsentCard.ContentType, null, card);
        att.Name = fileName;

        return att;
    }
}
