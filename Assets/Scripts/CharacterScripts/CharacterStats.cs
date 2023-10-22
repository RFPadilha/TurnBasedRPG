using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    //Variables are public to facilitate the creation of new unique characters
    public int strength = 10;
    public int vitality = 10;
    public int dexterity = 10;
    public int agility = 10;
    public int intelligence = 10;

    //Variables are public so its values can be accessed, but are regulated by this class
    public int hp { get; private set; }
    public int mp { get; private set; }
    public int dodge { get; private set; }
    public int hitRatio { get; private set; }
    public List<Skill> skillList;
    public void SetupStats()
    {
        hp = 5 + (2*vitality);
        mp = 10 + (3 * intelligence);
        dodge = 5 + (3 * agility);
        hitRatio = 20 + (4 * dexterity);
    }
}
