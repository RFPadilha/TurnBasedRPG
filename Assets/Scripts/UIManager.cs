using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*Script used to procedurally modify UI based on:
 * -which character is currently acting, be it on the player or enemy team
 * -which skills the acting player character has
 * -If a skill is being used, which targets are relevant to the skill
 * References the BattleHandler instance, because is needs access to battle informations in order to work properly
 * */
public class UIManager : MonoBehaviour
{
    private enum State
    {
        WaitingSelection,
        SelectedAttackAction,
        SelectedSkillAction,
        ChosenSkill,
        Acted
    }
    public static UIManager instance;

    //Objects setup via inspector window in the editor
    [SerializeField] GameObject battleOverWindow;
    [SerializeField] TextMeshProUGUI winnerText;

    [SerializeField] Button attackButton;
    [SerializeField] Button skillSelectionButton;

    [SerializeField] GameObject skillSubMenu;
    [SerializeField] GameObject targetSubMenu;

    [SerializeField] GameObject skillButtonPrefab;
    [SerializeField] GameObject targetButtonPrefab;

    //public variable used to facilitate debugging via console, in the current version can only be set via inspector
    public bool ShowUIDebugLogs = false;

    //variables used to control UI
    private State currentState;
    private BattleHandler battleHandler;
    private CharacterBattle actingCharacter;
    private bool battleIsOver = false;


    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        battleHandler = BattleHandler.GetInstance();
        currentState = State.WaitingSelection;
        HideBattleOverWindow();
        targetSubMenu.SetActive(false);
        skillSubMenu.SetActive(false);
        UpdateUI();
    }
    /*Update is used only to verify when battle should be over, in order to reduce processing costs
     * */
    private void Update()
    {
        if (battleIsOver) return;
        else
        {
            if (BattleHandler.GetInstance().enemiesDefeated)
            {
                ShowBattleOverWindow("Player won.\n Play Again?");
                battleIsOver = true;
            }
            else if (BattleHandler.GetInstance().playerDefeated)
            {

                ShowBattleOverWindow("Enemy won.\n Game Over");
                battleIsOver = true;
            }
        }

    }
    /*Instead of updating the UI constantly, I opted to implement this function to update only when necessary,
     * according to the current game state. This function also displays or hides the relevant menus based on
     * the current "stage" of action selection
     * */
    private void UpdateUI()
    {
        switch (currentState)
        {
            case State.WaitingSelection:
                SetMenuActivity(true);
                if (ShowUIDebugLogs) Debug.Log($"Waiting for {actingCharacter.name}'s turn.");
                break;

            case State.SelectedAttackAction:
                if (skillSubMenu.activeSelf) HideSkillMenu();
                if (!targetSubMenu.activeSelf) ShowTargetMenu();
                if (ShowUIDebugLogs) Debug.Log($"Choose {actingCharacter.name}'s target to attack.");
                break;

            case State.SelectedSkillAction:
                if (!skillSubMenu.activeSelf) ShowSkillMenu();
                if (ShowUIDebugLogs) Debug.Log($"Choose one of {actingCharacter.name}'s skills.");
                break;

            case State.ChosenSkill:
                if (!targetSubMenu.activeSelf) ShowTargetMenu();
                if (ShowUIDebugLogs) Debug.Log($"Choose {actingCharacter.name}'s skill target.");
                break;

            case State.Acted:
                HideSkillMenu();
                HideTargetMenu();

                    //ChooseNextActiveCharacter();
                if(battleHandler.state == BattleHandler.State.EnemyTurn)
                {
                    SetMenuActivity(false);
                    UpdateUI();
                }
                else
                {
                    battleHandler.ChooseNextActiveCharacter();
                    currentState = State.WaitingSelection;
                    UpdateUI();
                }
                break;
        }
    }
    /*Regulates when a player can act, and sets up acting character skill buttons
     * */
    private void SetMenuActivity(bool activity) 
    {
        attackButton.interactable = activity;
        skillSelectionButton.interactable = activity;
        if (activity)
        {
            actingCharacter = battleHandler.currentActingPlayerCharacter;
            SetupSkills(actingCharacter);
            if (ShowUIDebugLogs) Debug.Log($"{actingCharacter.name}'s skills were set in skill sub menu.");
        }
    }

    /*SelectedAttackAction and SelectedSkillAction are the only necessarily public function in order to set them up via editor,
     * since the "attack" and "skill" buttons don't require changing.
     * Through them, all other buttons are instantiated and configured procedurally based on the acting character skills and targets
     * */
    public void SelectedAttackAction()
    {
        currentState = State.SelectedAttackAction;
        SetupAttackTargets();
        UpdateUI();
    }
    public void SelectedSkillAction()
    {
        currentState = State.SelectedSkillAction;
        HideTargetMenu();
        UpdateUI();
    }
    /*Function associated with each skill selection button
     * */
    private void ChooseSkillAction(Skill chosenSkill)
    {
        currentState = State.ChosenSkill;
        SetupSkillTargets(chosenSkill);
        UpdateUI();
    }

    /*When its a player characters turn, sets up the skill selection menu with the acting characters skills,
     * */
    private void SetupSkills(CharacterBattle actingCharacter)
    {
        ClearButtonsInSubMenu(skillSubMenu.transform);
        foreach (Skill skill in actingCharacter.stats.skillList)
        {
            GameObject newSkillButton = Instantiate(skillButtonPrefab, skillSubMenu.transform);
            SkillButton newButton = newSkillButton.GetComponent<SkillButton>();
            newButton.skill = skill;
            newButton.skillDamage = skill.power;
            newSkillButton.GetComponentInChildren<TextMeshProUGUI>().text = skill.name + "\n(" + skill.mpConsumed + " mp)";
            if (skill.mpConsumed > actingCharacter.GetRemainingMana())
            {
                newSkillButton.GetComponent<Button>().interactable = false;
            }
            newSkillButton.GetComponent<Button>().onClick.AddListener(
                delegate 
                { 
                    ChooseSkillAction(newButton.skill); 
                });
            
        }
    }
    /*Function triggered when player chooses to attack
     * Displays available enemies to target and delegates the attack function from CharacterBattle,
     * associating it with the relevant target
     * */
    private void SetupAttackTargets()
    {
        ClearButtonsInSubMenu(targetSubMenu.transform);
        foreach (CharacterBattle target in battleHandler.GetLivingCharacters(battleHandler.enemies))
        {
            GameObject newTargetButton = Instantiate(targetButtonPrefab, targetSubMenu.transform);
            newTargetButton.GetComponent<TargetButton>().target = target;
            newTargetButton.GetComponentInChildren<TextMeshProUGUI>().text = target.name;
            newTargetButton.GetComponent<Button>().onClick.AddListener(
                delegate 
                { 
                    actingCharacter.Attack(target, () => 
                    {
                        //actingCharacter.hasActed = true;
                        currentState = State.Acted;
                        UpdateUI();
                    }); 
                });
        }
        if (ShowUIDebugLogs) Debug.Log($"Attack targets were set.");
    }

    /*Function triggered when player chooses which skill to use
     * Clears all previous targets and adds buttons for the relevant targets (team if its a healing skill, enemies if its damage)
     * Also delegates functions to be executed by each button when clicked
     * */
    private void SetupSkillTargets(Skill skill)
    {
        ClearButtonsInSubMenu(targetSubMenu.transform);
        if (skill.type == Skill.Type.Damage)
        {
            foreach (CharacterBattle target in battleHandler.GetLivingCharacters(battleHandler.enemies))
            {
                GameObject newTargetButton = Instantiate(targetButtonPrefab, targetSubMenu.transform);
                newTargetButton.GetComponent<TargetButton>().target = target;
                newTargetButton.GetComponentInChildren<TextMeshProUGUI>().text = target.name;

                if (skill.target == Skill.Target.All)
                {
                    CharacterBattle[] targetTeam = battleHandler.GetTargetTeam(target);
                    newTargetButton.GetComponent<Button>().onClick.AddListener(//applies skill effect to all targets, spending mana only once
                        delegate 
                        {
                            actingCharacter.SpendMana(skill.mpConsumed);
                            for (int i = 0; i < targetTeam.Length; i++)
                            {
                                actingCharacter.UseSkill(targetTeam[i], () => {}, skill);
                            }
                            //actingCharacter.hasActed = true;
                            currentState = State.Acted;
                            UpdateUI();
                        });
                    
                }
                else//single target skill
                {
                    newTargetButton.GetComponent<Button>().onClick.AddListener(
                        delegate 
                        {
                            actingCharacter.SpendMana(skill.mpConsumed);
                            actingCharacter.UseSkill(target, () => 
                            {
                                //actingCharacter.hasActed = true;
                                currentState = State.Acted;
                                UpdateUI();
                            }, skill);
                        });
                }
                
            }
        }
        else//healing type skill
        {
            foreach (CharacterBattle target in battleHandler.GetLivingCharacters(battleHandler.playerCharacters))
            {
                GameObject newTargetButton = Instantiate(targetButtonPrefab, targetSubMenu.transform);
                newTargetButton.GetComponent<TargetButton>().target = target;
                newTargetButton.GetComponentInChildren<TextMeshProUGUI>().text = target.name;
                newTargetButton.GetComponent<Button>().onClick.AddListener(
                    delegate { 
                        actingCharacter.UseSkill(target, () => 
                        {
                            actingCharacter.SpendMana(skill.mpConsumed);
                            //actingCharacter.hasActed = true;
                            currentState = State.Acted;
                            UpdateUI();
                        }, skill); 
                    });
            }
        }
        if (ShowUIDebugLogs) Debug.Log($"{skill.name} targets were set");
    }

    /*Given a submenu, such as the skill selection or target selection, destroys all buttons in order
     * to correctly assign new skills or targets
     * */
    public void ClearButtonsInSubMenu(Transform subMenu)
    {
        if (subMenu.childCount <= 0) return;
        foreach (Transform child in subMenu)
        {
            Destroy(child.gameObject);
        }
        if (ShowUIDebugLogs) Debug.Log($"{subMenu.name} buttons were destroyed to be replaced correctly.");
    }
    /*Menus visibility functions, only hide or display the menu in its name
     * */
    private void ShowTargetMenu()
    {
        targetSubMenu.SetActive(true);
    }
    private void HideTargetMenu()
    {
        targetSubMenu.SetActive(false);
    }
    private void ShowSkillMenu()
    {
        skillSubMenu.SetActive(true);
    }
    private void HideSkillMenu()
    {
        skillSubMenu.SetActive(false);
    }
    public void HideBattleOverWindow()
    {
        battleOverWindow.SetActive(false);
    }
    public void ShowBattleOverWindow(string text)
    {
        battleOverWindow.SetActive(true);
        winnerText.SetText(text);
    }

}