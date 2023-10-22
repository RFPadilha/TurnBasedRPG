using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManaBar : MonoBehaviour
{
    private ManaSystem manaSystem;
    public void SetUp(ManaSystem manaSystem)
    {
        this.manaSystem = manaSystem;
        manaSystem.onManaChanged += ManaSystem_OnManaChanged;
    }
    //Function triggered together with onManaChanged event on manaSystem
    private void ManaSystem_OnManaChanged(object sender, System.EventArgs e)
    {
        transform.Find("Bar").localScale = new Vector3(manaSystem.GetManaPercent(), 1);
    }
}
