# SqlAssistant - Teams bot

This Teams bot takes user queries in natural language form and generates & runs SQL queries on the database provided in the app setting "Sql_ConnectionString", returns the results in the chat. Expots the results to excel or Emails the excel optionally.
This uses OpenAI GPT-4 and Semantic Kernel SDK.

### App settings:
To run this bot configure these parameters in the appsettings.json
1. BOT_ID: Bot Id.
2. BOT_PASSWORD: Bot password
3. OpenAI_GPTModelName: Azure OpenAI GPT model name
4. OpenAI_EndPoint: Azure OpenAI endpoint
5. OpenAI_Key: Azure OpenAI key
6. Sql_ConnectionString: Database connection string
7. ApplicationInsights_ConnectionString: Azure Application Insights connection string for telemetry and logs
8. Graph_EmailFrom: Sender Email id
9. Graph_EmailTo: Recipient Email id
10. Graph_ClientId: Azure app registration client id to use with Graph API
11. Graph_ClientSecret: Azure app registration client sexret 
12. Graph_TenantId: Azure app registration tenenant id