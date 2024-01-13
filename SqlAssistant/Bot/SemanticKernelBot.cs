// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning;
using SqlAssistant.Plugins;

namespace SqlAssistant.Bot;

public class SemanticKernelBot : StateManagementBot
{
    private Kernel _kernel;
    private FunctionCallingStepwisePlanner _planner;
    private ConfigOptions _configOptions;
    private readonly SqlConnectionFactory _sqlConnectionFactory;
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
            _kernel.ImportPluginFromObject(new ExcelExportPlugin(turnContext, conversationData, _excelBuilder), "ExcelExportPlugin");
        }

        _kernel.ImportPluginFromObject(new EmailPlugin(_configOptions, _graphApiEmailClient, turnContext), "EmailPlugin");
    }

    protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
    {
        await turnContext.SendActivityAsync(new Activity()
        {
            Type = "message",
            Text = "Welcome to SQLAssistant Bot! Ask me anything to get started.",
            SuggestedActions = new SuggestedActions()
            {
                Actions = new List<string> { "How many customers are in the AdventureWorksLT database", "What are the top 5 highly sold products" }
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
            MaxIterations = 5,
            MaxTokens = 8000,
        };

        _planner = new FunctionCallingStepwisePlanner(stepwiseConfig);
        string prompt = FormatConversationHistory(conversationData);

        _logger.LogInformation(new EventId(1000, "BeginPlanExecution"), "{Prompt}", prompt);

        var result = await _planner.ExecuteAsync(_kernel, prompt);

        _logger.LogInformation(new EventId(1001, "EndPlanExecution"), "{Final Answer} | {Chat history}", result.FinalAnswer, System.Text.Json.JsonSerializer.Serialize(result.ChatHistory));

        if (String.IsNullOrWhiteSpace(result.FinalAnswer))
            return "Completed task";

        return result.FinalAnswer;
    }
}