// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.AI.OpenAI;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Planning;
using Newtonsoft.Json;
using SqlAssistant;
using SqlAssistant.Plugins;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Options;

namespace SqlAssistant.Bot;

public class SemanticKernelBot : StateManagementBot
{
    private Kernel _kernel;
    private FunctionCallingStepwisePlanner _planner;
    private ConfigOptions _configOptions;
    private readonly AzureOpenAITextEmbeddingGenerationService _embeddingsClient;
    private readonly SqlConnectionFactory _sqlConnectionFactory;
    private readonly string _welcomeMessage;
    private readonly List<string> _suggestedQuestions;
    private readonly string _systemMessage;
    private readonly ILogger<SemanticKernelBot> _logger;
    private readonly GraphApiEmailClient _graphApiEmailClient;
    private readonly ExcelBuilder _excelBuilder;

    public SemanticKernelBot(
        ConfigOptions configOptions,
        ConversationState conversationState,
        UserState userState,
        Kernel kernel,
        ILogger<SemanticKernelBot> logger,
        IHttpClientFactory clientFactory,
        GraphApiEmailClient graphApiEmailClient,
        ExcelBuilder excelBuilder,
        SqlConnectionFactory sqlConnectionFactory = null) :
        base(configOptions, conversationState, userState, clientFactory)
    {
        _configOptions = configOptions;
        _logger = logger;
        _kernel = kernel;
        //_welcomeMessage = _configOptions.PROMPT_WELCOME_MESSAGE;
        //_suggestedQuestions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(_configOptions.PROMPT_SUGGESTED_QUESTIONS);
        _systemMessage = new StreamReader("./Prompts/StepWiseStep/GeneratePlan.yaml")
                            .ReadToEnd();
        //    .Replace("{{PROMPT_SYSTEM_MESSAGE}}", config.GetValue<string>("PROMPT_SYSTEM_MESSAGE"));

        _sqlConnectionFactory = sqlConnectionFactory;
        _graphApiEmailClient = graphApiEmailClient;
        _excelBuilder = excelBuilder;
    }

    private void ImportPlugins(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext)
    {
        _kernel.Plugins.Clear();
        if (_sqlConnectionFactory != null)
        {
            _kernel.ImportPluginFromObject(new SQLPlugin(conversationData, turnContext, _sqlConnectionFactory), "SQLPlugin");
            _kernel.ImportPluginFromObject(new ExcelExportPlugin(turnContext, _excelBuilder), "ExcelExportPlugin");
        }

        _kernel.ImportPluginFromObject(new EmailPlugin(_configOptions, _graphApiEmailClient, turnContext), "EmailPlugin");
    }

    protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
    {
        await turnContext.SendActivityAsync(new Activity()
        {
            Type = "message",
            Text = _welcomeMessage,
            SuggestedActions = new SuggestedActions()
            {
                Actions = _suggestedQuestions
                    .Select(value => new CardAction(type: "postBack", value: value))
                    .ToList()
            }
        });
    }

    public override async Task<string> ProcessMessage(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext)
    {
        await turnContext.SendActivityAsync(new Activity(type: "typing"));

        if (turnContext.Activity.Text.IsNullOrEmpty())
            return "";

        ImportPlugins(conversationData, turnContext);

        var stepwiseConfig = new FunctionCallingStepwisePlannerConfig
        {
            //GetPromptTemplate = () => _systemMessage,
            MaxIterations = 5,
            MaxTokens = 8000,
        };

        _planner = new FunctionCallingStepwisePlanner(stepwiseConfig);
        string prompt = FormatConversationHistory(conversationData);

        _logger.LogInformation(new EventId(1000, "BeginPlanExecution"), "{Prompt}", prompt);

        var result = await _planner.ExecuteAsync(_kernel, prompt);

        _logger.LogInformation(new EventId(1001, "EndPlanExecution"), "{Final Answer} | {Chat history}", result.FinalAnswer, System.Text.Json.JsonSerializer.Serialize(result.ChatHistory));

        //var replyText = $"FinalAnswer:\n{result.FinalAnswer}\nChat history:\n{System.Text.Json.JsonSerializer.Serialize(result.ChatHistory)}";
        //return replyText;
        return result.FinalAnswer;

        //var res = await kernel.RunAsync(plan);
        //var stepsTaken = JsonConvert.DeserializeObject<Step[]>(res.FunctionResults.First().Metadata["stepsTaken"].ToString());
        //return stepsTaken[stepsTaken.Length - 1].final_answer;
    }
}

//private Kernel GetKernel(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext)
//{
//    _loggerFactory = LoggerFactory.Create(builder =>
//    {
//        // Add OpenTelemetry as a logging provider
//        builder.AddOpenTelemetry(options =>
//        {
//            options.AddAzureMonitorLogExporter(options => options.ConnectionString = _config.GetValue<string>("ApplicationInsights_ConnectionString"));
//            // Format log messages. This is default to false.
//            options.IncludeFormattedMessage = true;
//        });
//        builder.SetMinimumLevel(LogLevel.Information);
//    });

//    var builder = Kernel.CreateBuilder();
//    builder.Services.AddSingleton(_loggerFactory);

//    _kernel = builder.AddAzureOpenAIChatCompletion(_config.GetValue<string>("OpenAI_GPTModelName"), _config.GetValue<string>("OpenAI_EndPoint"), _config.GetValue<string>("OpenAI_Key"))
//                     .Build();

//    if (_sqlConnectionFactory != null)
//    {
//        _kernel.ImportPluginFromObject(new SQLPlugin(conversationData, turnContext, _sqlConnectionFactory), "SQLPlugin");
//    }

//    _kernel.ImportPluginFromObject(new ExcelExportPlugin(conversationData, turnContext), "ExcelExportPlugin");
//    return _kernel;
//}