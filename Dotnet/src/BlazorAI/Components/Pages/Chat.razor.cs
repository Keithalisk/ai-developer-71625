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
    private IMcpClient? mcpClient;
    private OpenAIPromptExecutionSettings openAIPromptExecutionSettings;

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

        // Challenge 04 - create an MCP client for the MS Learn Server
        // Create an MCPClient for the MSLearn server

        // Challenge 05 - Register Azure AI Foundry Text Embeddings Generation

        // Challenge 05 - Register Search Index

        // Challenge 07 - Add Azure AI Foundry Text To Image

        // Challenge 02 - Finalize Kernel Builder
        kernel = kernelBuilder.Build();

        // Challenge 04 - add a client for the MSLearn MCP Server
        /*
        if (mcpClient != null)
        {
            await mcpClient.DisposeAsync();
            mcpClient = null;
        }
        mcpClient = await McpClientFactory.CreateAsync(new SseClientTransport(new SseClientTransportOptions
        {
            Endpoint = new Uri(Configuration.GetValue<string>("MCP_ENDPOINT") ??
                               throw new Exception("MCP_ENDPOINT is not set in configuration")),
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
        // Challenge 03 - Add Time Plugin

        // Challenge 04 - Import OpenAPI Spec


        // Challenge 04 - Add the MCP Server tools
        /*
        var tools = await mcpClient.ListToolsAsync().ConfigureAwait(false);
        kernel.Plugins.AddFromFunctions("MSLearn", tools.Select(aiFunction => aiFunction.AsKernelFunction()));
        */

        // Challenge 05 - Add Search Plugin

        // Challenge 07 - Text To Image Plugin

    }

    private async Task SendMessage()
    {
        if (!string.IsNullOrWhiteSpace(newMessage) && chatHistory != null)
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
