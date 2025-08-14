using Azure;
using Azure.Core.Pipeline;
using Azure.Search.Documents.Indexes;
using BlazorAI.Plugins;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol;
using ModelContextProtocol.Client;

#pragma warning disable SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace BlazorAI.Components.Pages;

public partial class Chat
{
    private ChatHistory? chatHistory;
    private Kernel? kernel;
    private IMcpClient? mcpMSLearnClient;
    private IMcpClient? mcpPetStoreClient;
    private OpenAIPromptExecutionSettings? openAIPromptExecutionSettings;

    [Inject]
    public required IConfiguration Configuration { get; set; }
    [Inject]
    private ILoggerFactory LoggerFactory { get; set; } = null!;

    protected async Task InitializeSemanticKernel()
    {
        chatHistory = [];

        // Challenge 02 - Configure Semantic Kernel
        var kernelBuilder = Kernel.CreateBuilder();

        // Challenge 02 - Add OpenAI Chat Completion
        kernelBuilder.AddAzureOpenAIChatCompletion(
            Configuration["AOI_DEPLOYMODEL"]!,
            Configuration["AOI_ENDPOINT"]!,
            Configuration["AOI_API_KEY"]!);

        // Add Logger for Kernel
        kernelBuilder.Services.AddSingleton(LoggerFactory);

        // Challenge 03 and 04 - Services Required
        kernelBuilder.Services.AddHttpClient();

        // Challenge 05 - Register Azure OpenAI Text Embeddings Generation
        kernelBuilder.AddAzureOpenAIEmbeddingGenerator(
            deploymentName: Configuration["EMBEDDINGS_DEPLOYMODEL"]!, // Name of deployment, e.g. "text-embedding-ada-002".
            endpoint: Configuration["AOI_ENDPOINT"]!,
            apiKey: Configuration["AOI_API_KEY"]!
        );

        // Challenge 05 - Register Search Index
        kernelBuilder.Services.AddSingleton<SearchIndexClient>(
            sp => new SearchIndexClient(
                new Uri(Configuration["AI_SEARCH_URL"]!),
                new AzureKeyCredential(Configuration["AI_SEARCH_KEY"]!)));
        kernelBuilder.Services.AddAzureAISearchVectorStore();

        // Challenge 07 - Add Azure AI Foundry Text To Image

        // Challenge 02 - Finalize Kernel Builder
        kernel = kernelBuilder.Build();

        // Challenge 04 - add a client for the MSLearn MCP Server
        /*
        if (mcpMSLearnClient != null)
        {
            await mcpMSLearnClient.DisposeAsync();
            mcpMSLearnClient = null;
        }
        mcpMSLearnClient = await McpClientFactory.CreateAsync(new SseClientTransport(new SseClientTransportOptions
        {
            Endpoint = new Uri(Configuration.GetValue<string>("MSLEARN_MCP_ENDPOINT") ??
                               throw new Exception("MSLEARN_MCP_ENDPOINT is not set in configuration")),
        }));
        */

        // Challenge 04 - add a client for the Petstore MCP Server via APIM
        /*
        if (mcpPetStoreClient != null)
        {
            await mcpPetStoreClient.DisposeAsync();
            mcpPetStoreClient = null;
        }
        mcpPetStoreClient = await McpClientFactory.CreateAsync(new SseClientTransport(new SseClientTransportOptions
        {
            Endpoint = new Uri(Configuration.GetValue<string>("PETSTORE_MCP_ENDPOINT") ??
                               throw new Exception("PETSTORE_MCP_ENDPOINT is not set in configuration")),
            TransportMode = HttpTransportMode.StreamableHttp,
            ConnectionTimeout = TimeSpan.FromSeconds(30)
        }));
        */

        // Challenge 03, 04, 05, & 07 - Add Plugins
        await AddPlugins();

        // Challenge 03 - Create OpenAIPromptExecutionSettings
        openAIPromptExecutionSettings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            ChatSystemPrompt = "You are an AI assistant that helps people find information.  Ask follow-up questions if something is unclear or you need more information to complete a task.",
            Temperature = 0.9f
        };

    }


    private async Task AddPlugins()
    {

        /* Remove this section to reduce tokens when demoing RAG
        // Challenge 03 - Add Time Plugin
        kernel.Plugins.AddFromType<TimePlugin>("TimePlugin");
        // Challenge 03 - Add Geocoding Plugin
        kernel.Plugins.AddFromObject(new GeocodingPlugin(kernel.Services.GetRequiredService<IHttpClientFactory>(), Configuration), "GeocodingPlugin");
        // Challenge 03 - Add Weather Plugin
        //kernel.Plugins.AddFromObject(new WeatherPlugin(kernel.Services.GetRequiredService<HttpClient>()), "WeatherPlugin");
        kernel.Plugins.AddFromObject(new WeatherPlugin(Http), "WeatherPlugin");

        // Challenge 04 - Import OpenAPI Spec
        await kernel.ImportPluginFromOpenApiAsync(
            pluginName: "workItems",
            uri: new Uri(new Uri(Configuration["WORKITEMS_BASE_URL"]!), Configuration["OPEN_API_DOC_ROUTE"]!));
        */
        // Challenge 04 - Add the MCP Server tools
        /*
        var tools = await mcpMSLearnClient.ListToolsAsync().ConfigureAwait(false);
        kernel.Plugins.AddFromFunctions("MSLearn", tools.Select(aiFunction => aiFunction.AsKernelFunction()));
        */
        /*
        var tools = await mcpPetStoreClient.ListToolsAsync().ConfigureAwait(false);
        kernel.Plugins.AddFromFunctions("PetStore", tools.Select(aiFunction => aiFunction.AsKernelFunction()));
        */

        // Challenge 05 - Add Search Plugin
        if (kernel != null)
        {
            kernel.Plugins.AddFromType<ContosoSearchPlugin>("ContosoSearchPlugin", kernel.Services);
        }


        // Challenge 07 - Text To Image Plugin

    }

    private async Task SendMessage()
    {
        if (!string.IsNullOrWhiteSpace(newMessage) && chatHistory != null && kernel != null && openAIPromptExecutionSettings != null)
        {
            // This tells Blazor the UI is going to be updated.
            StateHasChanged();
            loading = true;
            // Copy the user message to a local variable and clear the newMessage field in the UI
            var userMessage = newMessage;
            newMessage = string.Empty;
            StateHasChanged();

            // Challenge 02 - Retrieve the chat completion service
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            // Challenge 02 - Update Chat History
            chatHistory.AddUserMessage(userMessage);

            try
            {
                // Challenge 02 - Send a message to the chat completion service
                var response = await chatCompletionService.GetChatMessageContentsAsync(
                    chatHistory,
                    executionSettings: openAIPromptExecutionSettings,
                    kernel: kernel);

                // Challenge 02 - Add Response to the Chat History object
                chatHistory.AddRange(response);
            }
            catch (HttpOperationException e)
            {
                if (e.ResponseContent != null)
                {
                    chatHistory.AddAssistantMessage(e.ResponseContent);
                }
            }

            loading = false;
        }
    }
}
