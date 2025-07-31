using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using System.Text.Json;
using System.ComponentModel;

namespace BlazorAI.Plugins
{
	public class WeatherPlugin
	{
		private readonly HttpClient _httpClient;

		public WeatherPlugin(HttpClient httpClient)
		{
			_httpClient = httpClient;
		}

		[KernelFunction("get_weather_forecast")]
		[Description("Get the forecast weather at lat/long location for up to 16 days in the future.")]
		[return: Description("JSON object containing the forecasted weather conditions.")]	
		public async Task<string> GetForecastWeatherAsync(double latitude, double longitude, int days)
		{
			var url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,relative_humidity_2m,apparent_temperature,precipitation,rain,showers,snowfall,weather_code,wind_speed_10m,wind_direction_10m,wind_gusts_10m&hourly=temperature_2m,relative_humidity_2m,apparent_temperature,precipitation_probability,precipitation,rain,showers,snowfall,weather_code,cloud_cover,wind_speed_10m,uv_index&temperature_unit=fahrenheit&wind_speed_unit=mph&precipitation_unit=inch&forecast_days={days}";
			var response = await _httpClient.GetAsync(url);
			response.EnsureSuccessStatusCode();
			var content = await response.Content.ReadAsStringAsync();
			return content;
		}

		[KernelFunction("get_past_weather")]
		[Description("Get the past weather at lat/long location.")]
		[return: Description("JSON object containing the past weather conditions.")]
		public async Task<string> GetPastWeatherAsync(double latitude, double longitude, int daysInPast)
		{
			var url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&daily=weather_code,temperature_2m_max,temperature_2m_min,apparent_temperature_max,apparent_temperature_min,sunrise,sunset,daylight_duration,uv_index_max,precipitation_sum,rain_sum,showers_sum,snowfall_sum,precipitation_hours,wind_speed_10m_max,wind_gusts_10m_max&temperature_unit=fahrenheit&wind_speed_unit=mph&precipitation_unit=inch&past_days={daysInPast}";
			var response = await _httpClient.GetAsync(url);
			response.EnsureSuccessStatusCode();
			var content = await response.Content.ReadAsStringAsync();
			return content;
		}

	}
}
