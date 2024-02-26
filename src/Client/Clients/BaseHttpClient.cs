using Domain.Exceptions;
using Infrastructure.Models;
using Newtonsoft.Json;
using System.Text;

namespace Clients.Clients;

public class BaseHttpClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _urlBase;

    public BaseHttpClient(IHttpClientFactory httpClientFactory, string urlBase)
    {
        _urlBase = urlBase;
        _httpClientFactory = httpClientFactory;
    }

    public Uri GenerateUrl(string endpoint, Dictionary<string, string>? queryParameters = null)
    {
        StringBuilder urlBuilder = new($"{_urlBase}/{endpoint}");

        if (queryParameters?.Count > 0)
        {
            urlBuilder.Append('?');
            urlBuilder.Append(string.Join("&", queryParameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}")));
        }

        return new Uri(urlBuilder.ToString());
    }

    public static void AddHeaders(HttpRequestMessage request, Dictionary<string, string>? headerParameters = null)
    {
        if (headerParameters?.Count > 0)
        {
            foreach (var (key, value) in headerParameters)
            {
                request.Headers.Add(key, value);
            }
        }
    }
    private void DecodeResponseError(string body, HttpResponseMessage response)
    {
        string errorMessage = $"Failed to get data from client {_urlBase}, error code: {response.StatusCode}";

        try
        {
            ErrorModel? error = JsonConvert.DeserializeObject<ErrorModel>(body);

            if (error is null)
                errorMessage += $", with body: {body}";
            else
                errorMessage += $", with message: {error.Message}";

        }
        catch (Exception)
        {
            errorMessage += $", with body: {body}";
        }

        throw new ClientAPIException(errorMessage);
    }

    private T DecodeResponse<T>(string body)
    {
        return JsonConvert.DeserializeObject<T>(body)
            ?? throw new ClientAPIException($"Failed to deserialize data from client {_urlBase}, with body: {body}");
    }
    protected async Task<T> GetAsync<T>(string endpoint, Dictionary<string, string> queryParameters, Dictionary<string, string> headerParameters)
    {
        HttpClient client = _httpClientFactory.CreateClient();

        //configure
        Uri url = GenerateUrl(endpoint, queryParameters);
        HttpRequestMessage request = new(HttpMethod.Get, url);
        AddHeaders(request, headerParameters);

        //send
        HttpResponseMessage response = await client.SendAsync(request);

        //decode
        string responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            DecodeResponseError(responseBody, response);

        return DecodeResponse<T>(responseBody);
    }

    protected async Task<T> PostAsync<T>(string endpoint, Dictionary<string, string> queryParameters, Dictionary<string, string> headerParameters, T data)
    {
        HttpClient client = _httpClientFactory.CreateClient();

        //configure
        Uri url = GenerateUrl(endpoint, queryParameters);
        HttpRequestMessage request = new(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"),
        };
        AddHeaders(request, headerParameters);

        //send
        HttpResponseMessage response = await client.SendAsync(request);

        //decode
        string responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            DecodeResponseError(responseBody, response);

        return DecodeResponse<T>(responseBody);
    }

    protected async Task<T> PutAsync<T>(string endpoint, Dictionary<string, string> queryParameters, Dictionary<string, string> headerParameters, T data)
    {
        HttpClient client = _httpClientFactory.CreateClient();

        //configure
        Uri url = GenerateUrl(endpoint, queryParameters);
        HttpRequestMessage request = new(HttpMethod.Put, url)
        {
            Content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"),
        };
        AddHeaders(request, headerParameters);

        //send
        HttpResponseMessage response = await client.SendAsync(request);

        //decode
        string responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            DecodeResponseError(responseBody, response);

        return DecodeResponse<T>(responseBody);
    }
}
