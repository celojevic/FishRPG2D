using UnityEngine;

[CreateAssetMenu(menuName = "FishRPG/Class/Base")]
public class ClassBase : ScriptableObject
{

    [Header("Classes")]
    [Tooltip("Allows for a class to look different based on gender or anything else.")]
    public Appearance[] Appearances;
    public ItemBase[] StartingItems;
    public SpellBase[] StartingSpells;

    // regen %
    // spawn scene/pos

    // COMBAT
    // base dmg
    // base crit/multi
    // dmg type
    // scaling stat and %
    // base stats/ points
    // base attack speed

    // LEVEL UP
    //stats/points increase per level, flat or %


}

[System.Serializable]
public class Appearance
{

    public Gender Gender;

    public Sprite Sprite;

    public RuntimeAnimatorController Controller;

    [Tooltip("Sprite shown when in dialogue.")]
    public Sprite DialogueSprite;

}

public enum Gender : byte
{
    Male, Female, NonBinary
}
