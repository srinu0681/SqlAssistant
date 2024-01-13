using Microsoft.Extensions.Options;
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
                Graph_EmailFrom = "admin@0znz7.onmicrosoft.com",
                Graph_EmailTo = "yessrini8186@gmail.com",
                Graph_ClientId = "4dbffc13-31b5-4be9-bdbe-676e04d07dcb",
                Graph_ClientSecret = "nSI8Q~JdAxNbgE-fQfykiEGEObTfDXEuPs5G9aHF",
                Graph_TenantId = "204e09fd-f374-4702-9dd5-527fca27eb3d",
                Graph_GrantType = "client_credentials",
                Graph_Scope = "https://graph.microsoft.com/.default"
            };

            _mockOptions.Setup(options => options).Returns(_configOptions);
        }


        [Fact]
        public async Task SendsEmail()
        {
            var filePath = @"E:\wrk\Azure\OpenAI\SqlAssistant\SqlAssistant\Excel\databaseresults_162024-154856.xlsx";
            var bytes = File.ReadAllBytes(filePath);
            var graphEmailClient = new GraphApiEmailClient(_mockOptions.Object);
            await graphEmailClient.SendAsync(new List<string> { _configOptions.Graph_EmailTo }, "Query Results", "Database results", Path.GetFileName(filePath), bytes);
        }
    }
}