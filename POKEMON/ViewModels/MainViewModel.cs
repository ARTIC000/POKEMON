namespace POKEMON.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using System.Reactive;
using System.Threading;
using static POKEMON.Models.Pokemon;

public class MainViewModel : ReactiveObject
{
    private readonly HttpClient _client;
    private const int MaxRetries = 3;
    private const int InitialDelayMilliseconds = 1000;
    private CancellationTokenSource _cancellationTokenSource = new();

    public ObservableCollection<PokemonResponse> Pokemons { get; set; } = new();

    private string _pokemonQuery;
    private string _pokemonInfo;
    private string _pokemonImage;
    private double _progress;
    private string _statusMessage;

    private bool isSearching = false;

    public string PokemonQuery
    {
        get => _pokemonQuery;
        set => this.RaiseAndSetIfChanged(ref _pokemonQuery, value);
    }

    public string PokemonInfo
    {
        get => _pokemonInfo;
        set => this.RaiseAndSetIfChanged(ref _pokemonInfo, value);
    }

    public string PokemonImage
    {
        get => _pokemonImage;
        set => this.RaiseAndSetIfChanged(ref _pokemonImage, value);
    }

    public double Progress
    {
        get => _progress;
        set => this.RaiseAndSetIfChanged(ref _progress, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public ReactiveCommand<Unit, Unit> FetchPokemonCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelSearchCommand { get; }

    public MainViewModel()
    {
        _client = new HttpClient();
        StatusMessage = "...";

        FetchPokemonCommand = ReactiveCommand.CreateFromTask(FetchPokemonAsync);
        CancelSearchCommand = ReactiveCommand.Create(StopSearch);
    }

    private async Task FetchPokemonAsync()
    {
        if (string.IsNullOrWhiteSpace(PokemonQuery))
        {
            StatusMessage = "Please enter a Pokémon name, ID, or type.";
            return;
        }

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        isSearching = true;
        StatusMessage = "Searching...";
        Progress = 0;

        Pokemons.Clear();
        string[] queries = PokemonQuery.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        try
        {
            var tasks = queries.Select(async query =>
            {
                query = query.ToLower();
                if (IsNumeric(query))
                    await SearchByIdOrName(query, token);
                else
                    await SearchByNameOrType(query, token);
            });

            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Search canceled.";
        }
        finally
        {
            isSearching = false;
            StatusMessage = Pokemons.Any() ? "Pokémon found!" : "No Pokémon found.";
        }
    }


    private async Task SearchByIdOrName(string query, CancellationToken token)
    {
        int retryCount = 0;
        int delay = InitialDelayMilliseconds;

        while (retryCount < MaxRetries)
        {
            token.ThrowIfCancellationRequested();

            try
            {
                string url = $"https://pokeapi.co/api/v2/pokemon/{query}";
                var response = await _client.GetStringAsync(url, token);
                var pokemon = JsonSerializer.Deserialize<PokemonResponse>(response, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (pokemon != null)
                {
                    UpdatePokemonData(pokemon);
                    return;
                }

                StatusMessage = "Pokémon not found.";
                return;
            }
            catch (Exception ex)
            {
                retryCount++;
                StatusMessage = $"Attempt {retryCount}/{MaxRetries} failed. Retrying...";

                if (retryCount >= MaxRetries)
                {
                    StatusMessage = "Error: Unable to find Pokémon after multiple attempts.";
                    Console.WriteLine($"Final error: {ex.Message}");
                    return;
                }

                Console.WriteLine($"Retry {retryCount}/{MaxRetries} in {delay / 1000.0} seconds...");
                await Task.Delay(delay, token);
                delay *= 2;
            }
        }
    }

    private async Task SearchByNameOrType(string query, CancellationToken token)
    {
        int retryCount = 0;
        int delay = InitialDelayMilliseconds;

        while (retryCount < MaxRetries)
        {
            token.ThrowIfCancellationRequested();

            try
            {
                string url = $"https://pokeapi.co/api/v2/pokemon/{query}";
                var response = await _client.GetStringAsync(url, token);
                var pokemon = JsonSerializer.Deserialize<PokemonResponse>(response, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (pokemon != null)
                {
                    UpdatePokemonData(pokemon);
                    return;
                }
            }
            catch { }

            retryCount++;
            if (retryCount >= MaxRetries)
            {
                StatusMessage = "Pokémon not found. Checking if it's a type...";
                await SearchByType(query, token);
                return;
            }

            await Task.Delay(delay, token);
            delay *= 2;
        }
    }

    private async Task SearchByType(string type, CancellationToken token)
    {
        int retryCount = 0;
        int delay = InitialDelayMilliseconds;

        while (retryCount < MaxRetries)
        {
            token.ThrowIfCancellationRequested();

            try
            {
                string url = $"https://pokeapi.co/api/v2/type/{type}";
                var response = await _client.GetStringAsync(url, token);
                var jsonDoc = JsonDocument.Parse(response);
                var pokemonEntries = jsonDoc.RootElement.GetProperty("pokemon");

                Pokemons.Clear();
                foreach (var entry in pokemonEntries.EnumerateArray().Take(5))
                {
                    string pokemonName = entry.GetProperty("pokemon").GetProperty("name").GetString();
                    await SearchByIdOrName(pokemonName, token);
                }

                StatusMessage = Pokemons.Any() ? "Pokémon found!" : "No Pokémon found for this type.";
                return;
            }
            catch (Exception ex)
            {
                retryCount++;
                StatusMessage = $"Attempt {retryCount}/{MaxRetries} failed. Retrying...";

                if (retryCount >= MaxRetries)
                {
                    StatusMessage = "Error: Unable to find Pokémon type.";
                    Console.WriteLine($"Final error: {ex.Message}");
                    return;
                }

                await Task.Delay(delay, token);
                delay *= 2;
            }
        }
    }

    private void UpdatePokemonData(PokemonResponse pokemon)
    {
        PokemonInfo = $"Name: {pokemon.Name}\nHeight: {pokemon.Height}\nWeight: {pokemon.Weight}\n" +
                      $"Types: {string.Join(", ", pokemon.Types.Select(t => t.Type.Name))}";

        PokemonImage = pokemon.Sprites.FrontDefault;

        Pokemons.Add(pokemon);

        Progress = 100;
        StatusMessage = "Pokémon found!";
    }

    private void StopSearch()
    {
        _cancellationTokenSource.Cancel();
        isSearching = false;
        StatusMessage = "Search stopped.";
    }

    private bool IsNumeric(string value) => int.TryParse(value, out _);
}
