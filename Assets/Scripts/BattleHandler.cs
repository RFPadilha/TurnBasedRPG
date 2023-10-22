using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*Script used to manage all things battle-related:
 * -teams and characters
 * -turns and actions
 * -increasing or decreasing health and mana
 * The characters composing the player and enemy teams must be set through the Editor in the current version.
 * Even though the "CharacterBattle" script holds all the functions to manipulate and initialize character data,
 * this script is the starting point for all of them, in order to centralize power while individualizing responsibilities.
 * */
public class BattleHandler : MonoBehaviour
{
    private static BattleHandler instance;
    public static BattleHandler GetInstance()
    {
        return instance;
    }
    public enum State
    {
        PlayerTurn,
        EnemyTurn
    }
    //Inspector variables used to drag and drop character prefabs to instantiate
    [SerializeField] private Transform[] teamList;
    [SerializeField] private Transform[] enemyList;

    public bool showAllCharDebugs = false;
    public bool showBattleDebugs = false;

    public bool playerDefeated { get; private set; } = false;
    public bool enemiesDefeated { get; private set; } = false;

    //arrays used to control characters status, received from prefabs set via inspector
    public CharacterBattle[] playerCharacters { get; private set; }
    public CharacterBattle[] enemies { get; private set; }


    private CharacterBattle activeCharacter;
    public CharacterBattle currentActingPlayerCharacter { get; private set; }
    public State state { get; private set; }
    private int turnCount = 1;
    

    private void Awake()
    {
        instance = this;
    }
    
    /*Given the player and enemy character list, which must be set-up in the editor in the current version,
     * Spawns and initializes each character, while saving the main script of each one to better manipulate data.
     * */
    private void Start()
    {
        playerCharacters = new CharacterBattle[teamList.Length];
        enemies = new CharacterBattle[enemyList.Length];
        for (int i = 0; i < teamList.Length; i++)
        {
            playerCharacters.SetValue(SpawnCharacter(true, teamList[i], i), i);
            if (showAllCharDebugs) playerCharacters[i].showCharDebugs = true;
        }
        for (int i = 0; i < enemyList.Length; i++)
        {
            enemies.SetValue(SpawnCharacter(false, enemyList[i], i), i);
            if (showAllCharDebugs) enemies[i].showCharDebugs = true;
        }

        //starts on 1st character on player team
        if (showBattleDebugs) Debug.Log("Turn: 1");
        SetActiveCharacter(playerCharacters[0]);
        state = State.PlayerTurn;
    }
    /*Update was used during development only to speed up the testing process, current version doesn't use it
     * */
    private void Update()
    {
        /*
        if(state != State.PlayerTurn)
        {
            return;
        }
        //logic to regulate actions, was replaced by UI interactions
        if (Input.GetKeyDown(KeyCode.Space))
        {
            currentActingPlayerCharacter.hasActed = true;
            RandomizeAction(currentActingPlayerCharacter, RandomizeAmongLivingTargets(enemies), () =>{//this notation indicates a callback function to be performed after executing "Attack" function
                ChooseNextActiveCharacter();
            });
        }
        if (Input.GetMouseButtonDown(0))
        {
            DamagePopup.Create(Vector3.zero, 10, Color.red);
        }
        if (Input.GetMouseButtonDown(1))
        {
            DamagePopup.Create(Vector3.zero, 10, Color.green);
            currentActingPlayerCharacter.SpendMana(10);
        }
        */
    }
    /*Function to instantiate, place and initialize each character
     * */
    private CharacterBattle SpawnCharacter(bool isPlayerTeam, Transform characterPrefab, int offset)
    {
        Vector3 position;
        if (isPlayerTeam)
        {
            position = new Vector3(-2.5f, 0);
        }
        else
        {
            position = new Vector3(2.5f, 0);
        }
        Transform characterTransform = Instantiate(characterPrefab, position*(offset+1), Quaternion.identity);
        CharacterBattle character = characterTransform.GetComponent<CharacterBattle>();
        character.Setup(isPlayerTeam);
        if (isPlayerTeam)
        {
            if (showBattleDebugs) Debug.Log($"Spawned {characterPrefab.name} for player team.");
        }
        else
        {
            characterTransform.rotation = Quaternion.Euler(0, -180, 0);
            if (showBattleDebugs) Debug.Log($"Spawned {characterPrefab.name} for enemy team.");
        }
        return character;
    }

    #region Turn Structure Functions and Methods
    /*Finds next character to act, using the other functions to determine:
     * -if character is living
     * -if there are other characters on the team to act
     * -if the other team should act
     * -if the battle should end
     * regulating game state accordingly
     * */
    public void ChooseNextActiveCharacter()
    {
        if (TestBattleOver()) return;
        if (state == State.PlayerTurn)
        {
            NextCharacter(playerCharacters);
        }
        else if (state == State.EnemyTurn)
        {
            NextCharacter(enemies);
        }

    }


    /*Function to switch to next available characters in a team, switches acting team if there are none
     * */
    private void NextCharacter(CharacterBattle[] team)
    {
        int nextIndex = TeamHasTurns(team);
        Debug.Log($"Index: {nextIndex}");
        if (nextIndex < 0)
        {
            SwitchActingTeam(team);
        }
        else
        {
            Debug.Log($"Index: {nextIndex}");
            SetActiveCharacter(team[nextIndex]);
        }
    }


    /*Switches acting team and resets team "hasActed" status for its next turn after switching
     * */
    private void SwitchActingTeam(CharacterBattle[] team)
    {
        if (showBattleDebugs) Debug.Log($"Acting team has been switched.");

        if (GetNextLivingMember(team).isPlayerTeam)
        {
            if (showBattleDebugs) Debug.Log($"Enemy Turn: {turnCount}");
            StopAllCoroutines();
            for (int i = 0; i < enemies.Length; i++)
            {
                enemies[i].hasActed = false;
            }
            SetActiveCharacter(GetNextLivingMember(enemies));
            state = State.EnemyTurn;
        }
        else
        {
            turnCount++;
            if (showBattleDebugs) Debug.Log($"Turn: {turnCount}");
            StopAllCoroutines();
            for (int i = 0; i < playerCharacters.Length; i++)
            {
                playerCharacters[i].hasActed = false;
            }
            SetActiveCharacter(GetNextLivingMember(playerCharacters));
            state = State.PlayerTurn;
        }
    }


    /*Initializes a characters turn, disabling previous selection circle and enabling the new one
     * Applies all condition effects at the start of its turn, if it dies from one of these effects, 
     * chooses the next relevant character that can act.
     * If an enemy is supposed to act, randomizes target and action, choosing the next character to act when finished.
     * */
    private void SetActiveCharacter(CharacterBattle character)
    {
        
        if (activeCharacter != null)
        {
            if (showBattleDebugs) Debug.Log($"Next acting character has been chosen by {activeCharacter.name}.");
            activeCharacter.HideSelection();
        }
        activeCharacter = character;
        activeCharacter.UpdateConditions();
        if (activeCharacter.isDead) ChooseNextActiveCharacter();
        
        activeCharacter.ShowSelection();

        if (showBattleDebugs) Debug.Log($"{character.name} turn");
        if (!character.isPlayerTeam)
        {
            //activeCharacter.hasActed = true;
            RandomizeAction(activeCharacter, RandomizeAmongLivingTargets(playerCharacters), () => {
                ChooseNextActiveCharacter();
            });//this notation indicates a callback function to be performed after executing "Attack" function
            
        }
        else
        {
            currentActingPlayerCharacter = activeCharacter;
            Debug.Log($"{currentActingPlayerCharacter.name}'s turn");
        }
    }

     /*Verifies if there are characters that haven't yet acted on this turn
     * Returns character index if there is, 
     * -1 if there isn't, which indicates a switch to the other teams turn
     * */
    private int TeamHasTurns(CharacterBattle[] team)
    {
        for (int i = 0; i < team.Length; i++)
        {
            if (team[i].isDead) continue;
            if (team[i].hasActed == false)
            {
                return i;
            }
        }
        return -1;
    }


    /*Given a team, finds a living member, if none are found, verifies if battle should be over
     * */
    private CharacterBattle GetNextLivingMember(CharacterBattle[] team)
    {
        CharacterBattle nextLivingMember = team[0];
        if (!nextLivingMember.isDead)
        {
            //Debug.Log($"Chosen living member: {nextLivingMember.name}");
            return nextLivingMember;
        }
        for (int i = 0; i < team.Length; i++)
        {
            if (team[i].isDead) continue;
            nextLivingMember = team[i];
        }
        if (nextLivingMember.isDead)
        {
            TestBattleOver();
            return null;
        }
        //Debug.Log($"Chosen Living member: {nextLivingMember.name}");
        return nextLivingMember;
    }
    #endregion

    //---------------------------------------------------------------------------

    #region UI Helper Functions
    public List<CharacterBattle> GetLivingCharacters(CharacterBattle[] team)
    {
        List<CharacterBattle> livingCharacters = new List<CharacterBattle>();
        for (int i = 0; i < team.Length; i++)
        {
            if (team[i].isDead) continue;
            livingCharacters.Add(team[i]);
        }
        return livingCharacters;
    }
    public List<Skill> GetActingCharacterSkills()
    {
        return currentActingPlayerCharacter.stats.skillList;
    }
    #endregion

    //---------------------------------------------------------------------------

    #region Helper Functions
    /*Function used to randomize enemies actions and player actions during testing
     * Randomizes between attacking and the available skills with no preference
     * */
    private void RandomizeAction(CharacterBattle source, CharacterBattle target, Action onActionComplete)
    {
        int rand = Mathf.FloorToInt(UnityEngine.Random.Range(1, source.stats.skillList.Count + 1));
        Skill chosenSkill;
        if (rand == 0)
        {
            source.Attack(target, onActionComplete);
            if (showBattleDebugs) Debug.Log($"Randomizing enemy action resulted in {source.name} attacking {target.name}");
        }
        else
        {
            chosenSkill = source.stats.skillList.ToArray()[rand - 1];
            if (source.GetRemainingMana() < chosenSkill.mpConsumed)
            {
                source.Attack(target, onActionComplete);
            }
            else
            {
                CastSkill(source, target, chosenSkill, onActionComplete);
            }

        }
        Debug.Log("Choosing next action after randomizing");
    }
    /*Function used to target the skill being used, spends mana only once when casting skill, regardless of how many targets it affects
     * If its a healing skill, changes target to lowest health team member
     * */
    private void CastSkill(CharacterBattle source, CharacterBattle target, Skill skill, Action onActionComplete)
    {
        source.SpendMana(skill.mpConsumed);
        if (skill.target == Skill.Target.Single)
        {
            if (skill.type == Skill.Type.Healing)
            {
                target = ChooseLowerHealthTeamMember(source);
            }

            source.UseSkill(target, onActionComplete, skill);
            Debug.Log($"{gameObject.name} is calling actionComplete after {skill.name} skill");
        }
        else
        {
            if (skill.type == Skill.Type.Healing)
            {
                target = source;
            }

            CharacterBattle[] targetTeam = GetTargetTeam(target);

            for (int i = 0; i < targetTeam.Length; i++)
            {
                source.UseSkill(targetTeam[i], ()=> { }, skill);
            }
            onActionComplete();
            Debug.Log($"{gameObject.name} is calling actionComplete after {skill.name} skill");
        }
        if (showBattleDebugs) Debug.Log($"Randomizing enemy action resulted in {source.name} casting {skill.name} targeting {target.name}");
    }
    /*Given a character, return its team
     * */
    public CharacterBattle[] GetTargetTeam(CharacterBattle target)
    {
        if (target.isPlayerTeam) return playerCharacters;
        else return enemies;
    }
    /*Function used to prioritize healing in the relevant character team
     * */
    private CharacterBattle ChooseLowerHealthTeamMember(CharacterBattle source)
    {
        CharacterBattle chosenTeamMember = source;
        CharacterBattle[] team = GetTargetTeam(source);
        for (int i = 0; i < team.Length; i++)
        {
            if (team[i].isDead) continue;
            if (team[i].GetHealthPercent() < chosenTeamMember.GetHealthPercent())
            {
                chosenTeamMember = team[i];
            }
        }
        return chosenTeamMember;
    }
    /*Function to help in the targeting system, so characters won't waste actions attacking or healing dead characters
     * */
    private CharacterBattle RandomizeAmongLivingTargets(CharacterBattle[] team)
    {
        List<CharacterBattle> availableTargets = new List<CharacterBattle>();
        for (int i = 0; i < team.Length; i++)//builds list of available targets
        {
            if (team[i].isDead) continue;
            availableTargets.Add(team[i]);
        }
        int rand = Mathf.FloorToInt(UnityEngine.Random.Range(0, availableTargets.ToArray().Length));

        return availableTargets.ToArray()[rand];
    }
    /*Functions used to determine when a battle should end, determining the victorious team
     * */
    private bool TestBattleOver()
    {
        if (TeamWasDefeated(playerCharacters))
        {
            //player lost
            playerDefeated = true;
            return true;
        }
        if (TeamWasDefeated(enemies))
        {
            //player won
            enemiesDefeated = true;
            return true;
        }
        return false;
    }
    private bool TeamWasDefeated(CharacterBattle[] team)
    {
        for (int i = 0; i < team.Length; i++)
        {
            if (!team[i].IsDead()) return false;
        }
        return true;
    }
    #endregion
}
