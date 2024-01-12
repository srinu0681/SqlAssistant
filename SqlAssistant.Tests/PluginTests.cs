using Moq;

namespace SqlAssistant.Tests
{
    public class PluginTests
    {
        private ConfigOptions _configOptions;
        private readonly Mock<ConfigOptions> _mockOptions = new();

        public PluginTests()
        {
            _configOptions = new ConfigOptions
            {
                Graph_EmailFrom = "",
                Graph_EmailTo = "",
                Graph_ClientId = "",
                Graph_ClientSecret = "",
                Graph_TenantId = "",
                Graph_GrantType = "client_credentials",
                Graph_Scope = "https://graph.microsoft.com/.default"
            };

            _mockOptions.Setup(options => options).Returns(_configOptions);
        }


        [Fact]
        public async Task SendsEmail()
        {
            var filePath = @"databaseresults_162024-154856.xlsx";
            var bytes = File.ReadAllBytes(filePath);
            var graphEmailClient = new GraphApiEmailClient(_mockOptions.Object);
            await graphEmailClient.SendAsync(new List<string> { _configOptions.Graph_EmailTo }, "Query Results", "Database results", Path.GetFileName(filePath), bytes);
        }
    }
}