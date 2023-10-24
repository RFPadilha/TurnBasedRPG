using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManaSystem
{
    public event EventHandler onManaChanged;
    private int mana;
    private int maxMana;
    public ManaSystem(int maxMana)
    {
        this.maxMana = maxMana;
        mana = maxMana;
    }
    public int GetMana()
    {
        return mana;
    }
    public float GetManaPercent()
    {
        return (float)mana / maxMana;
    }


    //Functions that trigger "onManaChanged" event
    public void SetMana(int value)
    {
        this.mana = value;
        if (onManaChanged != null) onManaChanged(this, EventArgs.Empty);
    }
    public void Reduce(int manaSpent)
    {
        mana -= manaSpent;
        if (mana < 0) mana = 0;
        if (onManaChanged != null) onManaChanged(this, EventArgs.Empty);
    }
    public void Restore(int healAmount)
    {
        mana += healAmount;
        if (mana > maxMana) mana = maxMana;
        if (onManaChanged != null) onManaChanged(this, EventArgs.Empty);
    }
}
