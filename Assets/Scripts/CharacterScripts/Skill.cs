using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*New skills can be easily created via Editor, by clicking with the right mouse button and selecting the ScriptableObjects sub-menu
 * All its attributes are regulated by this class, and it was defined as described in the document, with minimal additional variables
 * */
[CreateAssetMenu(fileName = "Skill", menuName = "ScriptableObjects/Skill", order = 1)]
public class Skill : ScriptableObject
{
    public enum Type
    {
        Damage,
        Healing
    }
    public enum Target
    {
        Single,
        All
    }
    public Type type;
    public Target target;
    public int chanceToHit;
    public int mpConsumed;
    public int power { get; private set; }//power was defined in this manner because it is dependant on player stats, whereas other attributes are not
    public Condition inflictCondition;
    public void SetPower(int value)
    {
        power = value;
    }
}
