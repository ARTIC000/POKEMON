using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace POKEMON.Models
{
    public class Pokemon
    {
        public class PokemonResponse
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("height")]
            public int Height { get; set; }

            [JsonPropertyName("weight")]
            public int Weight { get; set; }

            [JsonPropertyName("sprites")]
            public Sprites Sprites { get; set; }

            [JsonPropertyName("types")]
            public List<PokemonTypeWrapper> Types { get; set; }
        }

        public class Sprites
        {
            [JsonPropertyName("front_default")]
            public string FrontDefault { get; set; }
        }

        public class PokemonTypeWrapper
        {
            [JsonPropertyName("type")]
            public PokemonType Type { get; set; }
        }

        public class PokemonType
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }
        }
    }
}