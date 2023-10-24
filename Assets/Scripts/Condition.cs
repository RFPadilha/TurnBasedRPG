using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*New conditions can be easily created via Editor, by clicking with the right mouse button and selecting the ScriptableObjects sub-menu
 * All its attributes are regulated by this class, and it was defined as described in the document, with minimal additional variables
 * */
[CreateAssetMenu(fileName = "Condition", menuName = "ScriptableObjects/Condition", order = 2)]
public class Condition : ScriptableObject
{
    public enum Type
    {
        Healing,
        Damage
    }
    public Type type;
    public int hitRatio;
    public int duration;
    public int remainingDuration { get; private set; }
    public int power { get; private set; }//power was defined in this manner because it is dependant on player stats, whereas other attributes are not
    public void SetPower(int value)
    {
        power = value;
        remainingDuration = duration;
    }
    public void DecreaseDuration()
    {
        remainingDuration--;
    }
    public void RefreshDuration()
    {
        remainingDuration = duration;
    }
}
