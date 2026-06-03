using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProyectoIntegradorNet10.Services
{
    /// <summary>
    /// Model for a single LocationIQ geocoding result.
    /// </summary>
    public class LocationIQResult
    {
        public string? place_id { get; set; }
        public string? lat { get; set; }
        public string? lon { get; set; }
        public string? display_name { get; set; }
        public string? type { get; set; }
    }

    /// <summary>
    /// Helper for geocoding addresses via the LocationIQ API.
    /// Usage: var results = await LocationIQHelper.SearchAddress("some address");
    /// </summary>
    public static class LocationIQHelper
    {
        // ════════════════════════════════════════════════════════════════
        //  SET YOUR API KEY HERE
        // ════════════════════════════════════════════════════════════════
        private const string ApiKey = "pk.c16ebac99bb94e969f67573af1b8e05d";
        private const string BaseUrl = "https://us1.locationiq.com/v1/search";

        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// Searches for an address using the LocationIQ API.
        /// Returns a list of possible matches with lat/lon and display name.
        /// </summary>
        public static async Task<List<LocationIQResult>> SearchAddress(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<LocationIQResult>();

            try
            {
                string url = $"{BaseUrl}?key={ApiKey}&q={Uri.EscapeDataString(query)}&format=json&limit=5";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var results = JsonSerializer.Deserialize<List<LocationIQResult>>(json, options);
                return results ?? new List<LocationIQResult>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LocationIQ error: {ex.Message}");
                throw;
            }
        }
    }
}
