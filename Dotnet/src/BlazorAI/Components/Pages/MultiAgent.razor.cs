using BlazorAI.Queue;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Magentic;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;


#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace BlazorAI.Components.Pages
{
    public partial class MultiAgent
    {
        private ChatHistory? chatHistory;
        private IChatCompletionService? chatCompletionService;
        private OpenAIPromptExecutionSettings? openAIPromptExecutionSettings;
        private Kernel? kernel;
        private InProcessRuntime? runtime;

        [Inject]
        public required IConfiguration Configuration { get; set; }

        [Inject]
        private IBackgroundTaskQueue _backgroundTaskQueue { get; set; } = null!;

        private List<Agent> Agents { get; set; } = [];

        private MagenticOrchestration? orchestration;


        protected void InitializeSemanticKernel()
        {
            chatHistory = [];

            var kernelBuilder = Kernel.CreateBuilder();

            kernelBuilder.AddAzureOpenAIChatCompletion(
                Configuration["AOI_DEPLOYMODEL"] ?? "gpt-35-turbo",
                Configuration["AOI_ENDPOINT"]!,
                Configuration["AOI_API_KEY"]!);

            kernelBuilder.Services.AddSingleton(LoggerFactory);

            kernel = kernelBuilder.Build();

            AddPlugins();

            CreateAgents();

            // Implement the orchestration using Magentic below
            orchestration = new MagenticOrchestration(
              new StandardMagenticManager(
                   kernel.GetRequiredService<IChatCompletionService>(),
                   new OpenAIPromptExecutionSettings())
              {
                  MaximumInvocationCount = 5,
              }, Agents.ToArray())
                    {
                        ResponseCallback = ResponseCallback,
            };

            // Verify we have agents before proceeding
            if (Agents.Count == 0)
            {
                throw new InvalidOperationException("No agents were created. Check agent creation logic.");
            }

        }

        private void CreateAgents()
        {
            if (kernel is null)
            {
                throw new InvalidOperationException("Kernel must be initialized before creating agents.");
            }
            
            // Clear existing agents
            Agents.Clear();

            // Append the agents to the Agents list
            // Create a Business Analyst Agent
            Agents.Add(new ChatCompletionAgent()
            {
                Name = "BusinessAnalyst", // Do not use spaces!
                Description = "Responsible for analyzing user requirements and creating comprehensive project documentation.",
                Instructions = """
			               You are a Business Analyst responsible for analyzing user requirements and creating comprehensive project documentation.

			               CRITICAL RULES:
			               - NEVER write any code or provide code examples
			               - NEVER suggest specific implementation details or technical solutions
			               - Your role is purely analytical and documentation-focused

			               Your responsibilities:
			               1. Analyze and clarify user requirements
			               2. Break down features into detailed functional requirements
			               3. Create user stories and acceptance criteria
			               4. Define project scope and deliverables
			               5. Estimate effort and provide timeline recommendations
			               6. Document business rules and constraints
			               7. Create a comprehensive requirements specification

			               Your output should include:
			               - Clear, non-technical requirement descriptions
			               - User stories with acceptance criteria
			               - Business logic and workflow descriptions
			               - Data requirements (what data is needed, not how to store it)
			               - Integration requirements (what systems need to connect)
			               - Success criteria for each feature

			               Remember: You analyze WHAT needs to be built, not HOW to build it.
			               """,
                Kernel = kernel
            });

            // Create a Software Engineer Agent
            Agents.Add(new ChatCompletionAgent()
            {
                Name = "SoftwareEngineer", // Do not use spaces!
                Description = "Responsible for implementing the technical solution based on the Business requirements.",
                Instructions = """
			               You are a Software Engineer responsible for implementing the technical solution based on the Business Analyst's requirements.

			               CRITICAL RULES:
			               - ONLY write code and provide technical implementation details
			               - Base your implementation strictly on the Business Analyst's requirements
			               - DO NOT change or add requirements - implement exactly what was specified

			               Your responsibilities:
			               1. Review and understand the functional requirements from the Business Analyst
			               2. Design the technical architecture and system components
			               3. Write complete, working code for all specified features
			               4. Include proper error handling and validation
			               5. Provide clear code comments and documentation
			               6. Suggest appropriate technology stack and frameworks
			               7. Create database schemas and data models if needed
			               8. Implement security best practices
			               9. Write unit tests for critical functionality

			               Your output should include:
			               - Complete source code files with proper structure
			               - Technical documentation and architecture diagrams
			               - Database schemas and data models
			               - API specifications and interfaces
			               - Configuration files and deployment instructions
			               - Unit tests and testing documentation

			               Remember: You implement HOW to build what the Business Analyst specified.
			               """,
                Kernel = kernel
            });

            // Create a Product Owner Agent
            Agents.Add(new ChatCompletionAgent()
            {
                Name = "ProductOwner", // Do not use spaces!
                Description = "Responsible for reviewing the Software Engineer's implementation and ensuring it meets all requirements from the Business Analyst.",
                Instructions = """
			               You are a Product Owner responsible for reviewing the Software Engineer's implementation and ensuring it meets all requirements from the Business Analyst.

			               CRITICAL RULES:
			               - Your job is to VERIFY the implementation matches the requirements
			               - ONLY approve if ALL requirements are fully met in the code
			               - Use "%APPR%" in your response ONLY when completely satisfied
			               - Be thorough in your review - check every requirement

			               Your responsibilities:
			               1. Review the Software Engineer's implementation against Business Analyst requirements
			               2. Verify all functional requirements are implemented correctly
			               3. Check for completeness - no missing features or functionality
			               4. Validate that the code follows good practices and standards
			               5. Test the solution conceptually to ensure it works as intended
			               6. Provide specific feedback on what needs to be fixed or improved
			               7. Only approve when the implementation is production-ready

			               Your review process:
			               - Go through each requirement from the Business Analyst systematically
			               - Check if the Software Engineer's code addresses each requirement
			               - Look for edge cases, error handling, and robustness
			               - Verify the code is complete and functional

			               Response format:
			               - If satisfied: Provide positive feedback and include "%APPR%" to signal completion
			               - If not satisfied: List specific issues that need to be addressed, DO NOT include "%APPR%"

			               Remember: You are the quality gate - only approve work that truly meets all requirements.
			               """,
                Kernel = kernel
            });

        }

        private void AddPlugins()
        {

        }

        // Implement the callback to handle agent responses
        private async ValueTask ResponseCallback(ChatMessageContent response)
        {
            // Imlement the logic to handle the response from the agents
            // Add agent responses to chat history so we can see the conversation build
            if (!string.IsNullOrWhiteSpace(response.Content))
            {
                if (response.AuthorName == "ProductOwner" && response.Content.Contains("%APPR%"))
                    loading = false;
                chatHistory!.Add(response);
                await InvokeAsync(StateHasChanged); // Update the UI to show the new message
            }

        }

        private async Task SendMessage()
        {
            if (orchestration is null)
            {
                throw new InvalidOperationException("The 'orchestration' field must be initialized before sending messages.");
            }

            // Copy the message from the user input - just like in Chat.razor.cs
            // This code grouping is used to handle the user input message and update the UI accordingly
            var userMessage = MessageInput;
            MessageInput = string.Empty;
            loading = true;
            // While the agent orchestration has its own chat history, we also maintain a local chat history for UI updates
            chatHistory!.AddUserMessage(userMessage);
            StateHasChanged();

            // Use the injected _backgroundTaskQueue instance to queue the background chat orchestration task
            // This allows the UI to remain responsive while the orchestration runs in the background
            await _backgroundTaskQueue.QueueBackgroundWorkItemAsync(async token =>
            {
                // Implement the runtime
                // SemanticKernel.Agents.Runtime.InProcess is used for in-process execution
                runtime = new InProcessRuntime();
                await runtime.StartAsync();

                try
                {
                    // Create a prompt for the orchestration, including the user message
                    var prompt = $"""
                        You are **Orchestrator**, the Magentic manager that supervises three specialist agents:

                        • **BusinessAnalyst** – analyses and documents user requirements.  
                        • **SoftwareEngineer** – designs and implements the technical solution.  
                        • **ProductOwner** – validates that the implementation satisfies every documented requirement.  

                        ## Workflow (follow strictly)  
                        1. **Route the user request to BusinessAnalyst**. Wait for its structured requirements output.  
                        2. **Pass the BusinessAnalyst output to SoftwareEngineer**. Wait for code and all technical artefacts.  
                        3. **Pass BOTH previous outputs to ProductOwner** for review.  
                        4. If ProductOwner’s reply includes **“%APPR%”**, the work is approved – return the full deliverable set to the user and stop.  
                        5. If “%APPR%” is **not** present, forward ProductOwner’s feedback to SoftwareEngineer, then repeat steps 2-3.  
                        6. Escalate with an error summary if approval is not achieved after **three** complete review cycles.

                        ## Operating rules  
                        - Always select exactly **one** agent for each turn and send only the information that agent needs.  
                        - Preserve all agent outputs verbatim when forwarding to the next agent so that full context is maintained.
                        - Never modify agent instructions; rely on their internal role definitions for behaviour control.
                        - You may add concise routing notes (e.g., “Routing to SoftwareEngineer for implementation”).  
                        - Maintain a short memory of the iteration count to enforce the three-cycle limit.

                        ## Success criterion  
                        Work is complete only when ProductOwner returns “%APPR%”. At that point, compile and deliver:  
                        - The BusinessAnalyst requirement specification.  
                        - The full SoftwareEngineer code/artefacts.  
                        - The ProductOwner approval note.

                        ---

                        ### USER_REQUEST  
                        {userMessage}
                        """;

                    // Invoke the orchestration with the prompt and runtime.
                    
                    var result = await orchestration.InvokeAsync(prompt, runtime!);

                    var finalResult = await result.GetValueAsync(TimeSpan.FromSeconds(600));
                    // Note the timeout is set to 600 seconds (10 minutes) to allow for longer processing times
                }
                catch (Exception ex)
                {
                    chatHistory.AddAssistantMessage($"Error: {ex.Message}");
                }
                finally
                {
                    // Ensure the runtime is disposed of properly
                    await runtime.RunUntilIdleAsync();

                    // Ensure the UI is updated after the orchestration completes
                    loading = false;                    
                    await InvokeAsync(StateHasChanged);

                }
            });
        }

    }
}
