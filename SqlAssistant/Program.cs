using SqlAssistant;
using SqlAssistant.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.SemanticKernel;
using Azure.Monitor.OpenTelemetry.Exporter;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration.Get<ConfigOptions>();
builder.Services.AddSingleton(config);

// build logger
builder.Services.AddSingleton(sp =>
{
    return LoggerFactory.Create(builder =>
    {
        // Add OpenTelemetry as a logging provider
        builder.AddOpenTelemetry(options =>
        {
            options.AddAzureMonitorLogExporter(options => options.ConnectionString = config.ApplicationInsights_ConnectionString);
            options.IncludeFormattedMessage = true;
        });
        builder.SetMinimumLevel(LogLevel.Information);
    });
});

builder.Services.AddControllers();
builder.Services.AddHttpClient("WebClient", client => client.Timeout = TimeSpan.FromSeconds(600));
builder.Services.AddHttpContextAccessor();

// Create the Bot Framework Authentication to be used with the Bot Adapter.
builder.Configuration["MicrosoftAppType"] = "MultiTenant";
builder.Configuration["MicrosoftAppId"] = config.BOT_ID;
builder.Configuration["MicrosoftAppPassword"] = config.BOT_PASSWORD;
builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

// Create the Bot Framework Adapter with error handling enabled.
builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

builder.Services.AddSingleton<IStorage, MemoryStorage>();

// The Bot needs an HttpClient to download and upload files.
builder.Services.AddHttpClient();

builder.Services.AddScoped<ExcelBuilder>();

// Create the User state passing in the storage layer.
IStorage storage = new MemoryStorage();
var userState = new UserState(storage);
builder.Services.AddSingleton(userState);

builder.Services.AddSingleton<GraphApiEmailClient>();

// Build Semantic Kernel
builder.Services.AddSingleton(sp =>
{
    var builder = Kernel.CreateBuilder();
    builder.Services.AddSingleton(sp.GetService<ILoggerFactory>());

    return builder.AddAzureOpenAIChatCompletion(config.OpenAI_GPTModelName, config.OpenAI_EndPoint, config.OpenAI_Key)
                  .Build();
});

// Create the Conversation state passing in the storage layer.
var conversationState = new ConversationState(storage);
builder.Services.AddSingleton(conversationState);

builder.Services.AddSingleton(new SqlConnectionFactory(config.Sql_ConnectionString));

// Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
builder.Services.AddTransient<IBot, SemanticKernelBot>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();