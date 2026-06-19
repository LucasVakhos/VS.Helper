using System.Net.Http.Json;
using GH.VpnService.Contracts.Vpn;

namespace GH.VpnService.Win.Services;

public sealed class VpnApiClient : IDisposable
{
    private readonly HttpClient _httpClient = new();

    public string BaseUrl
    {
        get => _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? string.Empty;
        set => _httpClient.BaseAddress = new Uri(value.TrimEnd('/') + "/");
    }

    public VpnApiClient(string baseUrl)
    {
        BaseUrl = baseUrl;
    }

    public async Task<VpnBootstrapResponse> GetBootstrapAsync(CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<VpnBootstrapResponse>("api/vpn/bootstrap", cancellationToken);
        return result ?? new VpnBootstrapResponse();
    }

    public async Task<IReadOnlyList<VpnClientListItem>> GetClientsAsync(CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<VpnClientListItem>>("api/vpn/clients", cancellationToken);
        return result ?? [];
    }

    public async Task<VpnClientResponse> CreateClientAsync(CreateVpnRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/vpn/create", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<VpnClientResponse>(cancellationToken);
        return result ?? throw new InvalidOperationException("Server returned empty VPN response.");
    }

    public async Task<string> GetConfigAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetStringAsync($"api/vpn/config/{clientId}", cancellationToken);
    }

    public async Task DisableClientAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"api/vpn/disable/{clientId}", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteClientAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/vpn/{clientId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
