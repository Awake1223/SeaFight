namespace SeaFight.UnoApp.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    public ApiService()
    {
        _httpClient = new HttpClient 
        {
            BaseAddress = new Uri("http://localhost:5224")
        };

    }
    

    public async Task<string> CreateGameAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/create");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();

        }
        catch(Exception ex) 
        {
            Console.WriteLine($"CORS error: {ex}");
            throw;
        }
    }
}
