// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using SqlAssistant;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace SqlAssistant.Bot;

public class StateManagementBot : TeamsActivityHandler
{
    private readonly ConfigOptions _configOptions;
    public readonly BotState _conversationState;
    public readonly BotState _userState;
    private int _max_messages;
    private int _max_attachments;
    private readonly IHttpClientFactory _clientFactory;

    public StateManagementBot(ConfigOptions configOptions, ConversationState conversationState, UserState userState, IHttpClientFactory clientFactory)
    {
        _configOptions = configOptions;
        _conversationState = conversationState;
        _userState = userState;
        _max_messages = _configOptions.CONVERSATION_HISTORY_MAX_MESSAGES;
        _max_attachments = _configOptions.MAX_ATTACHMENTS;
        _clientFactory = clientFactory;
    }

    public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
    {
        await base.OnTurnAsync(turnContext, cancellationToken);
        // Save any state changes that might have occurred during the turn.
        await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
    }

    protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
    {
        await turnContext.SendActivityAsync("Welcome to State Bot Sample. Type anything to get started.");
    }

    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        var conversationStateAccessors = _conversationState.CreateProperty<ConversationData>(nameof(ConversationData));
        var conversationData = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationData());

        var userStateAccessors = _userState.CreateProperty<UserProfile>(nameof(UserProfile));
        var userProfile = await userStateAccessors.GetAsync(turnContext, () => new UserProfile());

        // -- Special keywords
        // Clear conversation
        if (turnContext.Activity.Text != null && turnContext.Activity.Text.ToLower() == "clear") {
            conversationData.History.Clear();
            //conversationData.Attachments.Clear();
            await turnContext.SendActivityAsync("Conversation context cleared");
            return;
        }
        conversationData.History.Add(new ConversationTurn { Role = "user", Message = turnContext.Activity.Text });

        var replyText = await ProcessMessage(conversationData, turnContext);

        await turnContext.SendActivityAsync(replyText);

        conversationData.History.Add(new ConversationTurn { Role = "assistant", Message = replyText });

        if (turnContext.Activity.Text == null || turnContext.Activity.Text.ToLower() == "") {
            return;
        }

        conversationData.History = conversationData.History.GetRange(
            Math.Max(conversationData.History.Count - _max_messages, 0),
            Math.Min(conversationData.History.Count, _max_messages)
        );
    }

    public virtual async Task<string> ProcessMessage(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext) {
        await turnContext.SendActivityAsync(JsonSerializer.Serialize(conversationData.History));
        return $"This chat now contains {conversationData.History.Count} messages";
    }

    public string FormatConversationHistory(ConversationData conversationData) {
        string history = "";
        List<ConversationTurn> latestMessages = conversationData.History.GetRange(
            Math.Max(conversationData.History.Count - _max_messages, 0), 
            Math.Min(conversationData.History.Count, _max_messages)
        );
        foreach (ConversationTurn conversationTurn in latestMessages)
        {
            history += $"{conversationTurn.Role.ToUpper()}:\n{conversationTurn.Message}\n";
        }
        history += "ASSISTANT:";
        return history;
    }    

    protected override async Task OnTeamsFileConsentAcceptAsync(ITurnContext<IInvokeActivity> turnContext, FileConsentCardResponse fileConsentCardResponse, CancellationToken cancellationToken)
    {
        try
        {
            JToken context = JObject.FromObject(fileConsentCardResponse.Context);

            string filePath = context["filename"].ToString();
            long fileSize = new FileInfo(filePath).Length;
            var client = _clientFactory.CreateClient();
            using (var fileStream = File.OpenRead(filePath))
            {
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentLength = fileSize;
                fileContent.Headers.ContentRange = new ContentRangeHeaderValue(0, fileSize - 1, fileSize);
                await client.PutAsync(fileConsentCardResponse.UploadInfo.UploadUrl, fileContent, cancellationToken);
            }

            await FileUploadCompletedAsync(turnContext, fileConsentCardResponse, cancellationToken);
        }
        catch (Exception e)
        {
            await FileUploadFailedAsync(turnContext, e.ToString(), cancellationToken);
        }
    }

    protected override async Task OnTeamsFileConsentDeclineAsync(ITurnContext<IInvokeActivity> turnContext, FileConsentCardResponse fileConsentCardResponse, CancellationToken cancellationToken)
    {
        JToken context = JObject.FromObject(fileConsentCardResponse.Context);

        var reply = MessageFactory.Text($"Declined. The query results will not uploaded</b>.");
        reply.TextFormat = "xml";
        await turnContext.SendActivityAsync(reply, cancellationToken);
    }

    private async Task FileUploadCompletedAsync(ITurnContext turnContext, FileConsentCardResponse fileConsentCardResponse, CancellationToken cancellationToken)
    {
        var downloadCard = new FileInfoCard
        {
            UniqueId = fileConsentCardResponse.UploadInfo.UniqueId,
            FileType = fileConsentCardResponse.UploadInfo.FileType,
        };

        var asAttachment = new Attachment
        {
            Content = downloadCard,
            ContentType = FileInfoCard.ContentType,
            Name = fileConsentCardResponse.UploadInfo.Name,
            ContentUrl = fileConsentCardResponse.UploadInfo.ContentUrl,
        };

        var reply = MessageFactory.Text($"<b>Upload the query results excel sheet to your OneDrive successfully.</b>");
        reply.TextFormat = "xml";
        reply.Attachments = new List<Attachment> { asAttachment };

        await turnContext.SendActivityAsync(reply, cancellationToken);
    }

    private async Task FileUploadFailedAsync(ITurnContext turnContext, string error, CancellationToken cancellationToken)
    {
        var reply = MessageFactory.Text($"<b>Upload failed.</b> Error: <pre>{error}</pre>");
        reply.TextFormat = "xml";
        await turnContext.SendActivityAsync(reply, cancellationToken);
    }
}
