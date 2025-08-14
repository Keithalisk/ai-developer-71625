using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Azure.Search.Documents;
using Azure;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;

namespace BlazorAI.Plugins
{

	public class ContosoSearchPlugin
	{
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
        private readonly SearchIndexClient _indexClient;

        public ContosoSearchPlugin(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, SearchIndexClient indexClient)

        {
			_embeddingGenerator = embeddingGenerator;
			_indexClient = indexClient;
		}

        [KernelFunction("contoso_search")]
		[Description("Search documents for an employee of Contoso.")]
		public async Task<string> SearchAsync(string query)
		{
			// Convert string query to vector
			var embeddingResult = await _embeddingGenerator.GenerateAsync(query);
			ReadOnlyMemory<float> embedding = embeddingResult.Vector.ToArray();

            // Get client for search operations
            SearchClient searchClient = _indexClient.GetSearchClient("employeehandbook");

			// Configure request parameters
			VectorizedQuery vectorQuery = new(embedding);
			vectorQuery.Fields.Add("contentVector");

			SearchOptions searchOptions = new() { VectorSearch = new() { Queries = { vectorQuery } } };

			// Perform search request
			Response<SearchResults<IndexSchema>> response = await searchClient.SearchAsync<IndexSchema>(searchOptions);

			// Collect search results
			await foreach (SearchResult<IndexSchema> result in response.Value.GetResultsAsync())
			{
				return result.Document.Content; // Return text from first result
			}

			return string.Empty;
		}

		private sealed class IndexSchema
		{
			[JsonPropertyName("content")]
			public required string Content { get; set; }

			[JsonPropertyName("title")]
			public required string Title { get; set; }

			[JsonPropertyName("url")]
			public required string Url { get; set; }
		}

	}

}
