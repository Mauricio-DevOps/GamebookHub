using System;
using System.Collections.Generic;

namespace GamebookHub.Models
{
    public class Gamebook
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;           // ex: "a-caverna-de-akbal"
        public string? Description { get; set; }
        public string? CoverUrl { get; set; }
        public string AuthorId { get; set; } = string.Empty;       // Id do AspNetUsers
        public DateTime? PublishedAt { get; set; }
        public bool IsPublished { get; set; }

        // --- NOVO: definição genérica de ficha por Gamebook ---
        public CharacterSheetTemplate? CharacterSheet { get; set; } = new();

        public List<GameNode> Nodes { get; set; } = new();
    }

    // Template da ficha do personagem (definido pelo autor do Gamebook)
    public class CharacterSheetTemplate
    {
        public int Id { get; set; }
        public bool Enabled { get; set; } = false;                 // Tem ficha?
        public List<AttributeDefinition> Attributes { get; set; } = new();
        public InventoryConfig Inventory { get; set; } = new();    // Configs do inventário
    }

    // Atributo genérico (ex.: forca, defesa, inteligencia, hp, etc.)
    public class AttributeDefinition
    {
        public int Id { get; set; }

        public string Key { get; set; } = string.Empty;            // chave única por Gamebook (ex.: "forca")
        public string Label { get; set; } = string.Empty;          // rótulo exibido (ex.: "Força")

        public AttributeType Type { get; set; } = AttributeType.Integer;

        // Faixas aplicáveis a tipos numéricos/recurso
        public decimal? Min { get; set; }
        public decimal? Max { get; set; }
        public decimal? Default { get; set; }

        public bool Visible { get; set; } = true;                  // visível ao jogador?
        public int Order { get; set; } = 0;                        // ordenação na UI

        // Para tipo Enum: opções separadas por vírgula (ex.: "leve,médio,pesado")
        public string? EnumOptions { get; set; }
    }

    public enum AttributeType
    {
        Integer,
        Decimal,
        Boolean,
        Text,
        Enum,
        Resource
    }

    // Configuração do inventário do Gamebook
    public class InventoryConfig
    {
        public int Id { get; set; }
        public bool Enabled { get; set; } = false;                 // Usa inventário?
        public InventoryMode Mode { get; set; } = InventoryMode.Unlimited;

        // Se Mode == Slots -> usar Slots
        public int? Slots { get; set; }

        // Se Mode == Weight -> usar Capacity (peso/volume total)
        public decimal? Capacity { get; set; }
    }

    public enum InventoryMode
    {
        Unlimited,
        Slots,
        Weight
    }
}
