namespace SqlAssistant
{
    public class ConfigOptions
    {
        public string BOT_ID { get; set; }
        public string BOT_PASSWORD { get; set; }

        public int CONVERSATION_HISTORY_MAX_MESSAGES { get; set; }
        public int MAX_ATTACHMENTS { get; set; }

        public string OpenAI_GPTModelName { get; set; }
        public string OpenAI_EndPoint { get; set; }
        public string OpenAI_Key { get; set; }

        public string Sql_ConnectionString { get; set; }
        public string ApplicationInsights_ConnectionString { get; set; }

        public string Graph_ClientId { get; set; }
        public string Graph_ClientSecret { get; set; }
        public string Graph_TenantId { get; set; }
        public string Graph_GrantType { get; set; }
        public string Graph_Scope { get; set; }
        public string Graph_EmailFrom { get; set; }
        public string Graph_EmailTo { get; set; }
    }
}
