using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*Main script to manage individual characters
 * Utilizes other scripts to compose the entirety of a character, such as Skills, Conditions, HealthSystem and Character Stats.
 * */
[RequireComponent(typeof(CharacterStats))]
public class CharacterBattle : MonoBehaviour
{
    //Variables visible in inspector
    [SerializeField] Animator m_Animator;
    [SerializeField] private int attackHitChance = 60;
    public bool hasActed = false;//needs to be public so the battleHandler can "handle" the battle accordingly

    //variable used to show or hide each characters debug log, in order to avoid console flooding
    public bool showCharDebugs = false;
    public CharacterStats stats { get; private set; }
    public bool isPlayerTeam { get; private set; }
    public bool isDead { get; private set; } = false;
    private GameObject selectionCircle;

    private HealthSystem healthSystem;
    private HealthBar healthBar;
    private ManaSystem manaSystem;
    private ManaBar manaBar;

    private List<Condition> continuousEffects;

    //Animator variables
    private bool isIdle = true;
    private bool isAttacking = false;
    private bool castingOffensive = false;
    private bool castingHealing = false;
    private bool multiTarget = false;
    private bool gotHit = false;


    /*Awake is used to initialize variables and find relevant components used by this script
     * */
    private void Awake()
    {
        selectionCircle = transform.Find("Selection").gameObject;
        healthBar = transform.Find("HealthBar").gameObject.GetComponent<HealthBar>();
        manaBar = transform.Find("ManaBar").gameObject.GetComponent<ManaBar>();
        continuousEffects = new List<Condition>();
        stats = GetComponent<CharacterStats>();
        m_Animator = GetComponentInChildren<Animator>();
        HideSelection();
    }
    private void Update()
    {
        m_Animator.SetBool("IsIdle", isIdle);
        m_Animator.SetBool("IsAttacking", isAttacking);
        m_Animator.SetBool("CastingOffensive", castingOffensive);
        m_Animator.SetBool("CastingHealing", castingHealing);
        m_Animator.SetBool("MultiTarget", multiTarget);
        m_Animator.SetBool("GotHit", gotHit);
        m_Animator.SetBool("IsDead", isDead);
    }

    /*Function called to initialize character, setting up all systems and assigning teams
     * */
    public void Setup(bool isPlayerTeam)
    {
        this.isPlayerTeam = isPlayerTeam;
        if (isPlayerTeam)
        {
            this.gameObject.name = "Player" + this.gameObject.name;
            selectionCircle.GetComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/PlayerSelection");
        }
        else
        {
            this.gameObject.name = "Enemy" + this.gameObject.name;
            selectionCircle.GetComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/EnemySelection");
        }
        stats.SetupStats();
        SetupSkills();

        healthSystem = new HealthSystem(stats.hp);
        healthBar.SetUp(healthSystem);
        healthSystem.onHealthChanged += HealthSystem_OnHealthChanged;

        manaSystem = new ManaSystem(stats.mp);
        manaBar.SetUp(manaSystem);
        manaSystem.onManaChanged += ManaSystem_OnManaChanged;

        //resource bars are world objects, as such, they must face the camera
        healthBar.transform.LookAt(transform.position - Camera.main.transform.position);
        manaBar.transform.LookAt(transform.position - Camera.main.transform.position);
        if (showCharDebugs) Debug.Log($"{this.gameObject.name}'s systems were initialized.");

    }
    //Divided functions by region in order to better navigate code
    #region Resource Bars Management Functions
    public void Damage(int value)
    {
        healthSystem.Damage(value);
        DamagePopup.Create(transform.position, value, Color.red);
        if (showCharDebugs) Debug.Log($"{this.gameObject.name} received {value} damage.");
        if (healthSystem.IsDead())
        {
            /*game object could be destroyed here, but in an actual game
            * we would play some sort of death animation or even display the characters body on the floor
            * */
            Debug.Log($"{this.gameObject.name} has died!");
            isDead = true;
        }
        else
        {
            gotHit = true;
            Invoke("RegisterHitAnim",.5f);
        }
    }
    void RegisterHitAnim()
    {
        gotHit = false;
    }
    public bool IsDead()
    {
        return healthSystem.IsDead();
    }
    public void Heal(int value)
    {
        healthSystem.Heal(value);
        DamagePopup.Create(transform.position, value, Color.green);
        if (showCharDebugs) Debug.Log($"{this.gameObject.name} healed {value} HP.");
    }
    public void SpendMana(int value)
    {
        manaSystem.Reduce(value);
        if (showCharDebugs) Debug.Log($"{this.gameObject.name} spent {value} mp.");
    }
    //unused function but could be used by other skills in a future version of this project
    public void RecoverMana(int value)
    {
        manaSystem.Restore(value);
    }
    public float GetHealthPercent()
    {
        return healthSystem.GetHealthPercent();
    }
    public int GetRemainingMana()
    {
        return manaSystem.GetMana();
    }
    /*Events used to control healthbar and mana bar in-game
     * */
    private void HealthSystem_OnHealthChanged(object sender, System.EventArgs e)
    {
        healthBar.transform.Find("Bar").localScale = new Vector3(healthSystem.GetHealthPercent(), 1);
    }
    private void ManaSystem_OnManaChanged(object sender, System.EventArgs e)
    {
        manaBar.transform.Find("Bar").localScale = new Vector3(manaSystem.GetManaPercent(), 1);
    }
    #endregion

    //----------------------------------------------------------------------------------------

    #region Attack Functions
    /*Attack is called only by the BattleHandler
     * AttackDelay is used to insert a small delay between apllying the effects and switching turns, for ease of comprehension.
     * */
    public void Attack(CharacterBattle target, Action onActionComplete)
    {
        int damageAmount = stats.strength;
        hasActed = true;
        isIdle = false;
        isAttacking = true;
        StopAllCoroutines();
        StartCoroutine(AttackDelay(target, onActionComplete, damageAmount, 1f));
    }
    IEnumerator AttackDelay(CharacterBattle target, Action onActionComplete, int damageAmount, float waitTime)
    {
        isAttacking = true;
        if (CalculateHit(target, attackHitChance))
        {
            if (showCharDebugs) Debug.Log($"{gameObject.name} attacked {target.gameObject.name} for {damageAmount} damage");
            target.Damage(damageAmount);
            if (showCharDebugs) Debug.Log($"Health remaining: {target.healthSystem.GetHealth()}");
        }
        else
        {
            if (showCharDebugs) Debug.Log($"{gameObject.name} missed the attack");
        }
        yield return new WaitForSeconds(waitTime);
        isIdle = true;
        isAttacking = false;
        Debug.Log($"{gameObject.name} is calling actionComplete after attack");
        onActionComplete();
    }
    #endregion

    //----------------------------------------------------------------------------------------

    #region Skills and Conditions Functions
    /*UseSkill is called only by the BattleHandler
     * SkillDelay is used to insert a small delay between applying the effects and switching turns, for ease of comprehension
     * If inflicting a condition that was already inflicted, instead refreshes duration
     * */
    public void UseSkill(CharacterBattle target, Action onActionComplete, Skill skill)
    {
        hasActed = true;
        if (target.isDead) return;
        isIdle = false;
        StopAllCoroutines();
        StartCoroutine(SkillDelay(target, onActionComplete, skill, 1f));
    }
    IEnumerator SkillDelay(CharacterBattle target, Action onActionComplete, Skill skill, float waitTime)
    {
        if (skill.target == Skill.Target.All) multiTarget = true;
        if (skill.type == Skill.Type.Damage)
        {
            castingOffensive = true;
            if (CalculateHit(target, skill.chanceToHit))
            {
                if (showCharDebugs) Debug.Log($"{gameObject.name} used {skill.name} at {target.gameObject.name}, dealing {skill.power} damage");
                target.Damage(skill.power);

                if (skill.inflictCondition != null && skill.inflictCondition.type == Condition.Type.Damage)
                {
                    if (CalculateHit(target, skill.inflictCondition.hitRatio))
                    {
                        if (target.continuousEffects.Contains(skill.inflictCondition))
                        {
                            if (showCharDebugs) Debug.Log($"{target.name} has refreshed the {skill.inflictCondition.name} duration");
                            int refresh = target.continuousEffects.IndexOf(skill.inflictCondition);
                            target.continuousEffects.ToArray()[refresh].RefreshDuration();
                        }
                        else
                        {
                            if (showCharDebugs) Debug.Log($"{target.name} has been inflicted with the {skill.inflictCondition.name} condition");
                            target.continuousEffects.Add(skill.inflictCondition);
                        }
                    }
                    else
                    {
                        if (showCharDebugs) Debug.Log($"{target.name} has avoided {skill.name}'s additional {skill.inflictCondition.name} effect.");
                    }
                }
                if (showCharDebugs) Debug.Log($"Health remaining: {target.healthSystem.GetHealth()}");
            }
            else
            {
                if (showCharDebugs) Debug.Log($"{gameObject.name} missed the {skill.name} on {target.name}");
            }
        }
        else//if its not a damage type skill, its healing
        {
            castingHealing = true;
            if (showCharDebugs) Debug.Log($"{gameObject.name} used {skill.name} at {target.gameObject.name}, healing {skill.power} HP");
            
            target.Heal(skill.power);
            if (skill.inflictCondition != null)
            {
                if (target.continuousEffects.Contains(skill.inflictCondition))
                {
                    if (showCharDebugs) Debug.Log($"{target.name} has refreshed the {skill.inflictCondition.name} duration");
                    int refresh = target.continuousEffects.IndexOf(skill.inflictCondition);
                    target.continuousEffects.ToArray()[refresh].RefreshDuration();
                }
                else
                {
                    if (showCharDebugs) Debug.Log($"{target.name} has received the {skill.inflictCondition.name} effect");
                    target.continuousEffects.Add(skill.inflictCondition);
                }
            }
        }
        yield return new WaitForSeconds(waitTime);
        isIdle = true;
        castingHealing = false;
        castingOffensive = false;
        multiTarget = false;
        onActionComplete();
    }
    /*Loops through every skill added to the character prefab via Editor, to facilitate the creation of new characters.
     * If a skill inflicts a condition, defines their effect's power based on the condition name
     * */
    public void SetupSkills()
    {
        foreach (Skill skill in stats.skillList)
        {
            switch (skill.name)
            {
                case "Fireball":
                    skill.SetPower(2 + stats.intelligence);
                    break;
                case "Inferno":
                    skill.SetPower(2 * stats.intelligence);
                    break;
                case "Heal":
                    skill.SetPower(2 * stats.intelligence);
                    break;
                case "ContinuousHealing":
                    skill.SetPower(3);
                    break;
            }
            if (skill.inflictCondition != null)
            {
                SetupConditions(skill);
                if (showCharDebugs) Debug.Log($"Initialized {this.gameObject.name}'s {skill.name} skill and associated {skill.inflictCondition.name} condition.");
            }
            else if (showCharDebugs) Debug.Log($"Initialized {this.gameObject.name}'s {skill.name} skill.");
        }
    }
    public void SetupConditions(Skill skill)
    {
        if (skill.inflictCondition != null)
        {
            switch (skill.inflictCondition.name)
            {
                case "Regeneration":
                    skill.inflictCondition.SetPower(stats.intelligence);
                    break;
                case "Burning":
                    skill.inflictCondition.SetPower(2);
                    break;
            }
        }
    }
    /*Loops through all conditions that should be applied to the character
     * */
    public void UpdateConditions()
    {
        if (continuousEffects.Count <= 0) return;
        Condition effectToRemove = null;
        foreach (Condition effect in continuousEffects)
        {
            if (effect.duration < 0)
            {
                //permanent effect
                ApplyConditionEffects(effect);
            }
            else if(effect.remainingDuration > 0)
            {
                ApplyConditionEffects(effect);
                effect.DecreaseDuration();
                if (showCharDebugs) Debug.Log($"Remaining {effect.name} duration: {effect.remainingDuration} turns");
                if (effect.remainingDuration == 0)
                {
                    effectToRemove = effect;
                    if (showCharDebugs) Debug.Log($"Condition {effect.name} expired on {this.gameObject.name}.");
                }
            }
        }
        //removing of effects must happen outside "foreach" iteration to not cause issues with the enumerating process
        if(effectToRemove!=null) continuousEffects.Remove(effectToRemove);
    }
    private void ApplyConditionEffects(Condition condition)
    {
        if (condition.type == Condition.Type.Damage)
        {
            if (showCharDebugs) Debug.Log($"{this.gameObject.name} received {condition.power} damage from {condition.name} effect.");
            healthSystem.Damage(condition.power);
        }
        else//if its not a damaging condition, its healing
        {
            if (showCharDebugs) Debug.Log($"{this.gameObject.name} recovered {condition.power} health due to {condition.name}.");
            healthSystem.Heal(condition.power);
        }

        if (showCharDebugs) Debug.Log($"Health remaining: {healthSystem.GetHealth()}");
    }
    #endregion

    //----------------------------------------------------------------------------------------

    #region HelperFunctions

    /*Function used to calculate if an Attack or Skill hits, as described in the document
     * Simulates the roll of a hundred sided die, then applies relevant modifiers, much like a tabletop RPG
     * */
    public bool CalculateHit(CharacterBattle target, int chanceToHit)
    {
        int d100 = Mathf.FloorToInt(UnityEngine.Random.Range(1, 101));
        if (chanceToHit + stats.hitRatio - target.stats.dodge > d100)
        {
            return true;
        }
        return false;
    }
    /*Methods used to display which character is currently acting
     * */
    public void HideSelection()
    {
        selectionCircle.SetActive(false);
    }
    public void ShowSelection()
    {
        selectionCircle.SetActive(true);
    }
    #endregion
}
