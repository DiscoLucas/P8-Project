using System;

/// <summary>
/// IngredientType is the type of ingient use for the drink, if it is a mixedLiqued with spirit and sirup fx use MixedLiquid
/// </summary>
[Serializable]
public enum IngredientType
{
    Spirit,
    Mixer,
    Garnish,
    Sirup,
    MixedLiquid
}