using System;
using System.Collections.Generic;
using System.Linq;
using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Animation;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.CharacterActor;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Enemy;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.EventCommon;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.EventMap;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Flag;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Item;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Map;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.SkillCustom;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.SystemSetting;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Troop;
using RPGMaker.Codebase.CoreSystem.Knowledge.Enum;
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement;
using RPGMaker.Codebase.CoreSystem.Service.EventManagement;
using RPGMaker.Codebase.CoreSystem.Service.MapManagement;
using RPGMaker.Codebase.Editor.Common;
using RPGMaker.Codebase.Editor.Hierarchy;
using Region = RPGMaker.Codebase.Editor.Hierarchy.Enum.Region;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Services
{
    /// <summary>
    /// Provides unified data access for MCP handlers using RPGMaker Editor APIs.
    /// This service wraps the DatabaseManagementService, EventManagementService, and MapManagementService
    /// to provide CRUD operations while ensuring proper UI refresh.
    /// </summary>
    public class EditorDataService
    {
        private static EditorDataService _instance;
        public static EditorDataService Instance => _instance ??= new EditorDataService();

        // Access services via Hierarchy singleton
        private DatabaseManagementService DatabaseService => Hierarchy.databaseManagementService;
        private EventManagementService EventService => Hierarchy.eventManagementService;
        private MapManagementService MapService => Hierarchy.mapManagementService;

        #region Character Operations

        /// <summary>
        /// Load all character actors from the database.
        /// </summary>
        public List<CharacterActorDataModel> LoadCharacters()
        {
            return DatabaseService.LoadCharacterActor();
        }

        /// <summary>
        /// Load a character by UUID.
        /// </summary>
        public CharacterActorDataModel LoadCharacterById(string uuid)
        {
            var characters = DatabaseService.LoadCharacterActor();
            return characters.FirstOrDefault(c => c.uuId == uuid);
        }

        /// <summary>
        /// Create a new character actor with proper defaults.
        /// Based on CharacterHierarchy.CreateCharacterActorDataModel()
        /// </summary>
        public CharacterActorDataModel CreateCharacter(string name = null, int charaType = 0)
        {
            var characters = DatabaseService.LoadCharacterActor();

            // Count existing characters of this type
            var createNum = characters.Count(c => c.charaType == charaType);

            // Generate default name if not provided
            var defaultName = name ?? $"#{(createNum + 1):D4}　{EditorLocalize.LocalizeText("WORD_1518")}";

            // Create with defaults using the data model's CreateDefault
            var newCharacter = CharacterActorDataModel.CreateDefault(
                Guid.NewGuid().ToString(),
                defaultName,
                charaType
            );

            // Set default class if actor type
            if (charaType == (int)ActorTypeEnum.ACTOR)
            {
                var classes = DatabaseService.LoadCharacterActorClass();
                if (classes.Count > 0)
                {
                    newCharacter.basic.classId = classes[0].id;
                }
            }

            // Set default images
            try
            {
                var faceImages = ImageManager.GetImageNameList(PathManager.IMAGE_FACE);
                if (faceImages.Count > 0)
                    newCharacter.image.face = faceImages[0];

                var moveChars = ImageManager.GetSvIdList(AssetCategoryEnum.MOVE_CHARACTER);
                if (moveChars.Count > 0)
                    newCharacter.image.character = moveChars[0].id;

                var battleChars = ImageManager.GetSvIdList(AssetCategoryEnum.SV_BATTLE_CHARACTER);
                if (battleChars.Count > 0)
                    newCharacter.image.battler = battleChars[0].id;

                var advImages = ImageManager.GetImageNameList(PathManager.IMAGE_ADV);
                if (advImages.Count > 0)
                    newCharacter.image.adv = advImages[0];
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to set default images: {ex.Message}");
            }

            characters.Add(newCharacter);
            DatabaseService.SaveCharacterActor(characters);

            RefreshHierarchy(Region.Character);

            return newCharacter;
        }

        /// <summary>
        /// Update an existing character.
        /// </summary>
        public CharacterActorDataModel UpdateCharacter(string uuid, Action<CharacterActorDataModel> updateAction)
        {
            var characters = DatabaseService.LoadCharacterActor();
            var character = characters.FirstOrDefault(c => c.uuId == uuid);

            if (character == null)
                throw new InvalidOperationException($"Character with UUID '{uuid}' not found.");

            updateAction(character);
            DatabaseService.SaveCharacterActor(characters);

            RefreshHierarchy(Region.Character);

            return character;
        }

        /// <summary>
        /// Delete a character by UUID.
        /// Based on CharacterHierarchy.DeleteCharacterActorDataModel()
        /// </summary>
        public void DeleteCharacter(string uuid)
        {
            var characters = DatabaseService.LoadCharacterActor();
            var character = characters.FirstOrDefault(c => c.uuId == uuid);

            if (character == null)
                throw new InvalidOperationException($"Character with UUID '{uuid}' not found.");

            characters.Remove(character);
            DatabaseService.SaveCharacterActor(characters);

            // Update initial party if needed (similar to CharacterHierarchy logic)
            var system = DatabaseService.LoadSystem();
            var actorCharacters = characters.Where(c => c.charaType == (int)ActorTypeEnum.ACTOR).ToList();

            if (actorCharacters.Count < system.initialParty.partyMax)
            {
                // Remove deleted character from party
                system.initialParty.party.RemoveAll(p => p == uuid);
                system.initialParty.partyMax = system.initialParty.party.Count;
                DatabaseService.SaveSystem(system);
            }
            else if (system.initialParty.party.Contains(uuid))
            {
                // Find a replacement character
                var replacement = actorCharacters.FirstOrDefault(c => !system.initialParty.party.Contains(c.uuId));
                if (replacement != null)
                {
                    for (int i = 0; i < system.initialParty.party.Count; i++)
                    {
                        if (system.initialParty.party[i] == uuid)
                        {
                            system.initialParty.party[i] = replacement.uuId;
                            break;
                        }
                    }
                    DatabaseService.SaveSystem(system);
                }
            }

            RefreshHierarchy(Region.Character);
        }

        #endregion

        #region Item Operations

        /// <summary>
        /// Load all items from the database.
        /// </summary>
        public List<ItemDataModel> LoadItems()
        {
            return DatabaseService.LoadItem();
        }

        /// <summary>
        /// Load an item by UUID.
        /// </summary>
        public ItemDataModel LoadItemById(string uuid)
        {
            var items = DatabaseService.LoadItem();
            return items.FirstOrDefault(i => i.basic.id == uuid);
        }

        /// <summary>
        /// Create a new item with proper defaults.
        /// Based on EquipHierarchy.CreateItemDataModel()
        /// </summary>
        public ItemDataModel CreateItem(string name = null)
        {
            var items = DatabaseService.LoadItem();

            var defaultName = name ?? $"#{(items.Count + 1):D4}　{EditorLocalize.LocalizeText("WORD_1518")}";

            var newItem = ItemDataModel.CreateDefault(Guid.NewGuid().ToString());
            newItem.basic.name = defaultName;

            // Set required default values (from EquipHierarchy.CreateItemDataModel)
            // アイテムタイプは通常
            newItem.basic.itemType = 1;
            // 範囲は味方の単体
            newItem.targetEffect.targetTeam = 2;
            newItem.targetEffect.targetRange = 0;

            items.Add(newItem);
            DatabaseService.SaveItem(items);

            RefreshHierarchy(Region.Equip);

            return newItem;
        }

        /// <summary>
        /// Update an existing item.
        /// </summary>
        public ItemDataModel UpdateItem(string uuid, Action<ItemDataModel> updateAction)
        {
            var items = DatabaseService.LoadItem();
            var item = items.FirstOrDefault(i => i.basic.id == uuid);

            if (item == null)
                throw new InvalidOperationException($"Item with UUID '{uuid}' not found.");

            updateAction(item);
            DatabaseService.SaveItem(items);

            RefreshHierarchy(Region.Equip);

            return item;
        }

        /// <summary>
        /// Delete an item by UUID.
        /// </summary>
        public void DeleteItem(string uuid)
        {
            var items = DatabaseService.LoadItem();
            var item = items.FirstOrDefault(i => i.basic.id == uuid);

            if (item == null)
                throw new InvalidOperationException($"Item with UUID '{uuid}' not found.");

            items.Remove(item);
            DatabaseService.SaveItem(items);

            RefreshHierarchy(Region.Equip);
        }

        #endregion

        #region Animation Operations

        /// <summary>
        /// Load all animations from the database.
        /// </summary>
        public List<AnimationDataModel> LoadAnimations()
        {
            return DatabaseService.LoadAnimation();
        }

        /// <summary>
        /// Load an animation by ID.
        /// </summary>
        public AnimationDataModel LoadAnimationById(string id)
        {
            var animations = DatabaseService.LoadAnimation();
            return animations.FirstOrDefault(a => a.id == id);
        }

        /// <summary>
        /// Create a new animation with proper defaults.
        /// Based on AnimationHierarchy.CreateAnimationDataModel()
        /// </summary>
        public AnimationDataModel CreateAnimation(string name = null)
        {
            var animations = DatabaseService.LoadAnimation();

            var defaultName = name ?? $"#{(animations.Count + 1):D4} {EditorLocalize.LocalizeText("WORD_1518")}";

            var newAnimation = AnimationDataModel.CreateDefault(Guid.NewGuid().ToString());
            newAnimation.particleName = defaultName;

            // Set required default values (from AnimationHierarchy.CreateAnimationDataModel)
            try
            {
                var battleEffects = ImageManager.GetSvIdList(AssetCategoryEnum.BATTLE_EFFECT);
                if (battleEffects.Count > 0)
                    newAnimation.particleId = battleEffects[0].id;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to set default particle: {ex.Message}");
            }
            newAnimation.offset = "10;10";
            newAnimation.rotation = "0;0;0";

            animations.Add(newAnimation);
            DatabaseService.SaveAnimation(animations);

            RefreshHierarchy(Region.Animation);

            return newAnimation;
        }

        /// <summary>
        /// Update an existing animation.
        /// </summary>
        public AnimationDataModel UpdateAnimation(string id, Action<AnimationDataModel> updateAction)
        {
            var animations = DatabaseService.LoadAnimation();
            var animation = animations.FirstOrDefault(a => a.id == id);

            if (animation == null)
                throw new InvalidOperationException($"Animation with ID '{id}' not found.");

            updateAction(animation);
            DatabaseService.SaveAnimation(animations);

            RefreshHierarchy(Region.Animation);

            return animation;
        }

        /// <summary>
        /// Delete an animation by ID.
        /// </summary>
        public void DeleteAnimation(string id)
        {
            var animations = DatabaseService.LoadAnimation();
            var animation = animations.FirstOrDefault(a => a.id == id);

            if (animation == null)
                throw new InvalidOperationException($"Animation with ID '{id}' not found.");

            animations.Remove(animation);
            DatabaseService.SaveAnimation(animations);

            RefreshHierarchy(Region.Animation);
        }

        #endregion

        #region Enemy Operations

        /// <summary>
        /// Load all enemies from the database.
        /// </summary>
        public List<EnemyDataModel> LoadEnemies()
        {
            return DatabaseService.LoadEnemy();
        }

        /// <summary>
        /// Load an enemy by ID.
        /// </summary>
        public EnemyDataModel LoadEnemyById(string id)
        {
            var enemies = DatabaseService.LoadEnemy();
            return enemies.FirstOrDefault(e => e.id == id);
        }

        /// <summary>
        /// Create a new enemy with proper defaults.
        /// Based on BattleHierarchy.CreateEnemyDataModel()
        /// </summary>
        public EnemyDataModel CreateEnemy(string name = null)
        {
            var enemies = DatabaseService.LoadEnemy();

            var defaultName = name ?? $"#{(enemies.Count + 1):D4}　{EditorLocalize.LocalizeText("WORD_1518")}";

            var newEnemy = EnemyDataModel.CreateDefault(Guid.NewGuid().ToString(), defaultName);

            // Set required default values (from BattleHierarchy.CreateEnemyDataModel)
            try
            {
                var enemyImages = ImageManager.GetImageNameList(PathManager.IMAGE_ENEMY);
                if (enemyImages.Count > 0)
                    newEnemy.images.image = enemyImages[0];
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to set default enemy image: {ex.Message}");
            }

            enemies.Add(newEnemy);
            DatabaseService.SaveEnemy(enemies);

            RefreshHierarchy(Region.Battle);

            return newEnemy;
        }

        /// <summary>
        /// Update an existing enemy.
        /// </summary>
        public EnemyDataModel UpdateEnemy(string id, Action<EnemyDataModel> updateAction)
        {
            var enemies = DatabaseService.LoadEnemy();
            var enemy = enemies.FirstOrDefault(e => e.id == id);

            if (enemy == null)
                throw new InvalidOperationException($"Enemy with ID '{id}' not found.");

            updateAction(enemy);
            DatabaseService.SaveEnemy(enemies);

            RefreshHierarchy(Region.Battle);

            return enemy;
        }

        /// <summary>
        /// Delete an enemy by ID.
        /// </summary>
        public void DeleteEnemy(string id)
        {
            var enemies = DatabaseService.LoadEnemy();
            var enemy = enemies.FirstOrDefault(e => e.id == id);

            if (enemy == null)
                throw new InvalidOperationException($"Enemy with ID '{id}' not found.");

            enemies.Remove(enemy);
            DatabaseService.SaveEnemy(enemies);

            RefreshHierarchy(Region.Battle);
        }

        #endregion

        #region Troop Operations

        /// <summary>
        /// Load all troops from the database.
        /// </summary>
        public List<TroopDataModel> LoadTroops()
        {
            var troops = DatabaseService.LoadTroop();
            // Filter out special troops
            return troops.Where(t =>
                t.id != TroopDataModel.TROOP_PREVIEW &&
                t.id != TroopDataModel.TROOP_BTATLE_TEST &&
                t.id != TroopDataModel.TROOP_AUTOMATCHING
            ).ToList();
        }

        /// <summary>
        /// Load a troop by ID.
        /// </summary>
        public TroopDataModel LoadTroopById(string id)
        {
            var troops = DatabaseService.LoadTroop();
            return troops.FirstOrDefault(t => t.id == id);
        }

        /// <summary>
        /// Create a new troop with proper defaults.
        /// Based on BattleHierarchy.CreateTroopDataModel()
        /// </summary>
        public TroopDataModel CreateTroop(string name = null)
        {
            var troops = DatabaseService.LoadTroop();
            var enemies = DatabaseService.LoadEnemy();

            var defaultName = name ?? $"#{(troops.Count + 1):D4}　{EditorLocalize.LocalizeText("WORD_1518")}";

            // Use TroopDataModel.CreateDefault() for proper initialization
            var newTroop = TroopDataModel.CreateDefault();
            newTroop.name = defaultName;

            // Set default enemy in members if enemies exist (from BattleHierarchy.CreateTroopDataModel)
            if (enemies.Count > 0)
            {
                var firstEnemyId = enemies[0].id;
                newTroop.sideViewMembers = new List<TroopDataModel.SideViewMember>
                {
                    TroopDataModel.SideViewMember.CreateDefault(firstEnemyId)
                };
                newTroop.frontViewMembers = new List<TroopDataModel.FrontViewMember>
                {
                    TroopDataModel.FrontViewMember.CreateDefault(firstEnemyId)
                };
            }

            troops.Add(newTroop);
            DatabaseService.SaveTroop(troops);

            RefreshHierarchy(Region.Battle);

            return newTroop;
        }

        /// <summary>
        /// Update an existing troop.
        /// </summary>
        public TroopDataModel UpdateTroop(string id, Action<TroopDataModel> updateAction)
        {
            var troops = DatabaseService.LoadTroop();
            var troop = troops.FirstOrDefault(t => t.id == id);

            if (troop == null)
                throw new InvalidOperationException($"Troop with ID '{id}' not found.");

            updateAction(troop);
            DatabaseService.SaveTroop(troops);

            RefreshHierarchy(Region.Battle);

            return troop;
        }

        /// <summary>
        /// Delete a troop by ID.
        /// </summary>
        public void DeleteTroop(string id)
        {
            var troops = DatabaseService.LoadTroop();
            var troop = troops.FirstOrDefault(t => t.id == id);

            if (troop == null)
                throw new InvalidOperationException($"Troop with ID '{id}' not found.");

            troops.Remove(troop);
            DatabaseService.SaveTroop(troops);

            RefreshHierarchy(Region.Battle);
        }

        #endregion

        #region Skill Operations

        /// <summary>
        /// Load all custom skills from the database.
        /// </summary>
        public List<SkillCustomDataModel> LoadSkills()
        {
            return DatabaseService.LoadSkillCustom();
        }

        /// <summary>
        /// Load a skill by ID.
        /// </summary>
        public SkillCustomDataModel LoadSkillById(string id)
        {
            var skills = DatabaseService.LoadSkillCustom();
            return skills.FirstOrDefault(s => s.basic.id == id);
        }

        /// <summary>
        /// Create a new skill with proper defaults.
        /// Based on SkillHierarchy.CreateSkillCustomDataModel()
        /// </summary>
        public SkillCustomDataModel CreateSkill(string name = null)
        {
            var skills = DatabaseService.LoadSkillCustom();
            var system = DatabaseService.LoadSystem();

            var defaultName = name ?? $"#{(skills.Count + 1):D4}　{EditorLocalize.LocalizeText("WORD_1518")}";

            var newSkill = SkillCustomDataModel.CreateDefault(Guid.NewGuid().ToString());
            newSkill.basic.name = defaultName;

            // Set required default values (from SkillHierarchy.CreateSkillCustomDataModel)
            newSkill.basic.message = EditorLocalize.LocalizeText("WORD_0437");

            // スキルタイプが一つ以上あれば「なし」ではなく、一つ目を設定する
            if (system.skillTypes.Count > 0)
            {
                newSkill.basic.skillType = 1;
            }

            // 敵の単体
            newSkill.targetEffect.targetTeam = 1;
            newSkill.targetEffect.targetRange = 0;

            // 計算式の初期値を入れる
            try
            {
                var skillCommon = DatabaseService.LoadSkillCommon();
                if (skillCommon.Count > 0)
                {
                    // デフォルトの攻撃計算式を設定
                    newSkill.targetEffect.damage.value =
                        $"a.atk * {skillCommon[0].damage.normalAttack.aMag} - b.def * {skillCommon[0].damage.normalAttack.bMag}";
                    newSkill.userEffect.damage.value =
                        $"a.atk * {skillCommon[0].damage.normalAttack.aMag} - b.def * {skillCommon[0].damage.normalAttack.bMag}";
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to set default damage formula: {ex.Message}");
            }

            skills.Add(newSkill);
            DatabaseService.SaveSkillCustom(skills);

            RefreshHierarchy(Region.Skill);

            return newSkill;
        }

        /// <summary>
        /// Update an existing skill.
        /// </summary>
        public SkillCustomDataModel UpdateSkill(string id, Action<SkillCustomDataModel> updateAction)
        {
            var skills = DatabaseService.LoadSkillCustom();
            var skill = skills.FirstOrDefault(s => s.basic.id == id);

            if (skill == null)
                throw new InvalidOperationException($"Skill with ID '{id}' not found.");

            updateAction(skill);
            DatabaseService.SaveSkillCustom(skills);

            RefreshHierarchy(Region.Skill);

            return skill;
        }

        /// <summary>
        /// Delete a skill by ID.
        /// </summary>
        public void DeleteSkill(string id)
        {
            var skills = DatabaseService.LoadSkillCustom();
            var skill = skills.FirstOrDefault(s => s.basic.id == id);

            if (skill == null)
                throw new InvalidOperationException($"Skill with ID '{id}' not found.");

            skills.Remove(skill);
            DatabaseService.SaveSkillCustom(skills);

            RefreshHierarchy(Region.Skill);
        }

        #endregion

        #region System Settings

        /// <summary>
        /// Load system settings.
        /// </summary>
        public SystemSettingDataModel LoadSystemSettings()
        {
            return DatabaseService.LoadSystem();
        }

        /// <summary>
        /// Save system settings.
        /// </summary>
        public void SaveSystemSettings(SystemSettingDataModel settings)
        {
            DatabaseService.SaveSystem(settings);
            RefreshHierarchy(Region.Initialization);
        }

        /// <summary>
        /// Update system settings.
        /// </summary>
        public SystemSettingDataModel UpdateSystemSettings(Action<SystemSettingDataModel> updateAction)
        {
            var settings = DatabaseService.LoadSystem();
            updateAction(settings);
            DatabaseService.SaveSystem(settings);
            RefreshHierarchy(Region.Initialization);
            return settings;
        }

        #endregion

        #region Flags Operations

        /// <summary>
        /// Load all flags (switches and variables).
        /// </summary>
        public FlagDataModel LoadFlags()
        {
            return DatabaseService.LoadFlags();
        }

        /// <summary>
        /// Save flags.
        /// </summary>
        public void SaveFlags(FlagDataModel flags)
        {
            DatabaseService.SaveFlags(flags);
            RefreshHierarchy(Region.FlagsEdit);
        }

        #endregion

        #region Common Event Operations

        /// <summary>
        /// Load all common events.
        /// </summary>
        public List<EventCommonDataModel> LoadCommonEvents()
        {
            return EventService.LoadEventCommon();
        }

        /// <summary>
        /// Load a common event by event ID.
        /// </summary>
        public EventCommonDataModel LoadCommonEventById(string eventId)
        {
            var events = EventService.LoadEventCommon();
            return events.FirstOrDefault(e => e.eventId == eventId);
        }

        /// <summary>
        /// Create a new common event.
        /// Based on CommonEventHierarchy.CreateEventCommonDataModel()
        /// </summary>
        public EventCommonDataModel CreateCommonEvent(string name = null)
        {
            var existingEvents = EventService.LoadEventCommon();

            // Create the event data model first (contains actual commands)
            var newEventModel = EventDataModel.CreateDefault();
            EventService.SaveEvent(newEventModel);

            // Create common event with default name
            var defaultName = name ?? $"#{(existingEvents.Count + 1):D4}　{EditorLocalize.LocalizeText("WORD_1518")}";

            var newModel = EventCommonDataModel.CreateDefault(newEventModel.id, defaultName);
            newModel.eventId = newEventModel.id;
            newModel.conditions.Add(new EventCommonDataModel.EventCommonCondition(0, ""));
            EventService.SaveEventCommon(newModel);

            RefreshHierarchy(Region.CommonEvent);

            return newModel;
        }

        /// <summary>
        /// Update an existing common event.
        /// </summary>
        public EventCommonDataModel UpdateCommonEvent(string eventId, Action<EventCommonDataModel> updateAction)
        {
            var events = EventService.LoadEventCommon();
            var commonEvent = events.FirstOrDefault(e => e.eventId == eventId);

            if (commonEvent == null)
                throw new InvalidOperationException($"Common event with eventId '{eventId}' not found.");

            updateAction(commonEvent);
            EventService.SaveEventCommon(commonEvent);

            RefreshHierarchy(Region.CommonEvent);

            return commonEvent;
        }

        /// <summary>
        /// Delete a common event.
        /// Based on CommonEventHierarchy.DeleteEventCommonDataModel()
        /// </summary>
        public void DeleteCommonEvent(string eventId)
        {
            var events = EventService.LoadEventCommon();
            var commonEvent = events.FirstOrDefault(e => e.eventId == eventId);

            if (commonEvent == null)
                throw new InvalidOperationException($"Common event with eventId '{eventId}' not found.");

            // Delete the underlying event data
            var eventDataModel = EventService.LoadEventById(eventId);
            if (eventDataModel != null)
            {
                EventService.DeleteEvent(eventDataModel);
            }

            // Delete the common event
            EventService.DeleteCommonEvent(commonEvent);

            RefreshHierarchy(Region.CommonEvent);
        }

        /// <summary>
        /// Duplicate a common event.
        /// Based on CommonEventHierarchy.DuplicateEventCommonDataModel()
        /// </summary>
        public EventCommonDataModel DuplicateCommonEvent(string eventId)
        {
            var events = EventService.LoadEventCommon();
            var originalCommonEvent = events.FirstOrDefault(e => e.eventId == eventId);

            if (originalCommonEvent == null)
                throw new InvalidOperationException($"Common event with eventId '{eventId}' not found.");

            // Clone the underlying event data
            var originalEventData = EventService.LoadEventById(eventId);
            if (originalEventData == null)
                throw new InvalidOperationException($"Event data for eventId '{eventId}' not found.");

            var duplicatedEventData = originalEventData.Clone();
            duplicatedEventData.id = Guid.NewGuid().ToString();
            EventService.SaveEvent(duplicatedEventData);

            // Clone the common event
            var duplicated = originalCommonEvent.Clone();
            duplicated.eventId = duplicatedEventData.id;

            // Generate unique name
            var eventNames = events.Select(e => e.name).ToList();
            duplicated.name = CreateDuplicateName(eventNames, duplicated.name);

            EventService.SaveEventCommon(duplicated);

            RefreshHierarchy(Region.CommonEvent);

            return duplicated;
        }

        /// <summary>
        /// Save a common event.
        /// </summary>
        public void SaveCommonEvent(EventCommonDataModel commonEvent)
        {
            EventService.SaveEventCommon(commonEvent);
            RefreshHierarchy(Region.CommonEvent);
        }

        #endregion

        #region Event Data Operations

        /// <summary>
        /// Load event data by ID.
        /// </summary>
        public EventDataModel LoadEventById(string eventId)
        {
            return EventService.LoadEventById(eventId);
        }

        /// <summary>
        /// Save event data.
        /// </summary>
        public void SaveEvent(EventDataModel eventData)
        {
            EventService.SaveEvent(eventData);
            RefreshHierarchy(Region.CommonEvent);
        }

        /// <summary>
        /// Update event data.
        /// </summary>
        public EventDataModel UpdateEvent(string eventId, Action<EventDataModel> updateAction)
        {
            var eventData = EventService.LoadEventById(eventId);

            if (eventData == null)
                throw new InvalidOperationException($"Event with ID '{eventId}' not found.");

            updateAction(eventData);
            EventService.SaveEvent(eventData);

            RefreshHierarchy(Region.CommonEvent);

            return eventData;
        }

        #endregion

        #region Map Operations

        /// <summary>
        /// Load all maps.
        /// </summary>
        public List<MapDataModel> LoadMaps()
        {
            return MapService.LoadMaps();
        }

        /// <summary>
        /// Load a map by ID.
        /// </summary>
        public MapDataModel LoadMapById(string id)
        {
            var maps = MapService.LoadMaps();
            return maps.FirstOrDefault(m => m.id == id);
        }

        /// <summary>
        /// Create a new map with proper defaults.
        /// </summary>
        public MapDataModel CreateMap(string name = null)
        {
            var newMap = MapService.CreateMapForEditor();

            if (!string.IsNullOrEmpty(name))
            {
                newMap.name = name;
                newMap.displayName = name;
                MapService.SaveMap(newMap, RPGMaker.Codebase.CoreSystem.Service.MapManagement.Repository.MapRepository.SaveType.NO_PREFAB);
            }

            RefreshHierarchy(Region.Map);

            return newMap;
        }

        /// <summary>
        /// Update an existing map.
        /// </summary>
        public MapDataModel UpdateMap(string id, Action<MapDataModel> updateAction)
        {
            var maps = MapService.LoadMaps();
            var map = maps.FirstOrDefault(m => m.id == id);

            if (map == null)
                throw new InvalidOperationException($"Map with ID '{id}' not found.");

            updateAction(map);
            MapService.SaveMap(map, RPGMaker.Codebase.CoreSystem.Service.MapManagement.Repository.MapRepository.SaveType.NO_PREFAB);

            RefreshHierarchy(Region.Map);

            return map;
        }

        /// <summary>
        /// Delete a map by ID.
        /// </summary>
        public void DeleteMap(string id)
        {
            var maps = MapService.LoadMaps();
            var map = maps.FirstOrDefault(m => m.id == id);

            if (map == null)
                throw new InvalidOperationException($"Map with ID '{id}' not found.");

            // Also delete associated map events
            var mapEvents = EventService.LoadEventMap().Where(e => e.mapId == id).ToList();
            foreach (var evt in mapEvents)
            {
                EventService.DeleteEventMap(evt);
            }

            MapService.RemoveMap(map);

            RefreshHierarchy(Region.Map);
        }

        /// <summary>
        /// Load all map events.
        /// </summary>
        public List<EventMapDataModel> LoadMapEvents()
        {
            return EventService.LoadEventMap();
        }

        /// <summary>
        /// Load map events for a specific map.
        /// </summary>
        public List<EventMapDataModel> LoadMapEventsByMapId(string mapId)
        {
            var events = EventService.LoadEventMap();
            return events.Where(e => e.mapId == mapId).ToList();
        }

        /// <summary>
        /// Load a map event by event ID.
        /// </summary>
        public EventMapDataModel LoadMapEventById(string eventId)
        {
            var events = EventService.LoadEventMap();
            return events.FirstOrDefault(e => e.eventId == eventId);
        }

        /// <summary>
        /// Create a new map event.
        /// </summary>
        public EventMapDataModel CreateMapEvent(string mapId, int x = 0, int y = 0)
        {
            var map = LoadMapById(mapId);
            if (map == null)
                throw new InvalidOperationException($"Map with ID '{mapId}' not found.");

            // Create event data model first
            var eventData = EventDataModel.CreateDefault();
            EventService.SaveEvent(eventData);

            // Create map event with the position
            var mapEvent = new EventMapDataModel
            {
                eventId = eventData.id,
                mapId = mapId,
                x = x,
                y = y,
                name = "",
                note = "",
                temporaryErase = 0,
                pages = new List<EventMapDataModel.EventMapPage>()
            };

            // Add a default page using the CreateDefault method
            var defaultPage = EventMapDataModel.EventMapPage.CreateDefault();
            mapEvent.pages.Add(defaultPage);

            EventService.SaveEventMap(mapEvent);

            RefreshHierarchy(Region.Map);

            return mapEvent;
        }

        /// <summary>
        /// Update a map event.
        /// </summary>
        public EventMapDataModel UpdateMapEvent(string eventId, Action<EventMapDataModel> updateAction)
        {
            var events = EventService.LoadEventMap();
            var mapEvent = events.FirstOrDefault(e => e.eventId == eventId);

            if (mapEvent == null)
                throw new InvalidOperationException($"Map event with eventId '{eventId}' not found.");

            updateAction(mapEvent);
            EventService.SaveEventMap(mapEvent);

            RefreshHierarchy(Region.Map);

            return mapEvent;
        }

        /// <summary>
        /// Delete a map event.
        /// </summary>
        public void DeleteMapEvent(string eventId)
        {
            var events = EventService.LoadEventMap();
            var mapEvent = events.FirstOrDefault(e => e.eventId == eventId);

            if (mapEvent == null)
                throw new InvalidOperationException($"Map event with eventId '{eventId}' not found.");

            // Also delete the underlying event data
            var eventData = EventService.LoadEventById(eventId);
            if (eventData != null)
            {
                EventService.DeleteEvent(eventData);
            }

            EventService.DeleteEventMap(mapEvent);

            RefreshHierarchy(Region.Map);
        }

        #endregion

        #region Flag Extended Operations

        /// <summary>
        /// Get a specific switch by ID.
        /// </summary>
        public FlagDataModel.Switch GetSwitchById(string id)
        {
            var flags = DatabaseService.LoadFlags();
            return flags.switches?.FirstOrDefault(s => s.id == id);
        }

        /// <summary>
        /// Get a specific variable by ID.
        /// </summary>
        public FlagDataModel.Variable GetVariableById(string id)
        {
            var flags = DatabaseService.LoadFlags();
            return flags.variables?.FirstOrDefault(v => v.id == id);
        }

        /// <summary>
        /// Create a new switch.
        /// </summary>
        public FlagDataModel.Switch CreateSwitch(string name = null)
        {
            var flags = DatabaseService.LoadFlags();
            var newSwitch = FlagDataModel.Switch.CreateDefault();

            if (!string.IsNullOrEmpty(name))
            {
                newSwitch.name = name;
            }

            flags.switches ??= new List<FlagDataModel.Switch>();
            flags.switches.Add(newSwitch);
            DatabaseService.SaveFlags(flags);

            RefreshHierarchy(Region.FlagsEdit);

            return newSwitch;
        }

        /// <summary>
        /// Create a new variable.
        /// </summary>
        public FlagDataModel.Variable CreateVariable(string name = null)
        {
            var flags = DatabaseService.LoadFlags();
            var newVariable = FlagDataModel.Variable.CreateDefault();

            if (!string.IsNullOrEmpty(name))
            {
                newVariable.name = name;
            }

            flags.variables ??= new List<FlagDataModel.Variable>();
            flags.variables.Add(newVariable);
            DatabaseService.SaveFlags(flags);

            RefreshHierarchy(Region.FlagsEdit);

            return newVariable;
        }

        /// <summary>
        /// Update a switch.
        /// </summary>
        public FlagDataModel.Switch UpdateSwitch(string id, Action<FlagDataModel.Switch> updateAction)
        {
            var flags = DatabaseService.LoadFlags();
            var switchData = flags.switches?.FirstOrDefault(s => s.id == id);

            if (switchData == null)
                throw new InvalidOperationException($"Switch with ID '{id}' not found.");

            updateAction(switchData);
            DatabaseService.SaveFlags(flags);

            RefreshHierarchy(Region.FlagsEdit);

            return switchData;
        }

        /// <summary>
        /// Update a variable.
        /// </summary>
        public FlagDataModel.Variable UpdateVariable(string id, Action<FlagDataModel.Variable> updateAction)
        {
            var flags = DatabaseService.LoadFlags();
            var variableData = flags.variables?.FirstOrDefault(v => v.id == id);

            if (variableData == null)
                throw new InvalidOperationException($"Variable with ID '{id}' not found.");

            updateAction(variableData);
            DatabaseService.SaveFlags(flags);

            RefreshHierarchy(Region.FlagsEdit);

            return variableData;
        }

        /// <summary>
        /// Delete a switch.
        /// </summary>
        public void DeleteSwitch(string id)
        {
            var flags = DatabaseService.LoadFlags();
            var switchData = flags.switches?.FirstOrDefault(s => s.id == id);

            if (switchData == null)
                throw new InvalidOperationException($"Switch with ID '{id}' not found.");

            flags.switches.Remove(switchData);
            DatabaseService.SaveFlags(flags);

            RefreshHierarchy(Region.FlagsEdit);
        }

        /// <summary>
        /// Delete a variable.
        /// </summary>
        public void DeleteVariable(string id)
        {
            var flags = DatabaseService.LoadFlags();
            var variableData = flags.variables?.FirstOrDefault(v => v.id == id);

            if (variableData == null)
                throw new InvalidOperationException($"Variable with ID '{id}' not found.");

            flags.variables.Remove(variableData);
            DatabaseService.SaveFlags(flags);

            RefreshHierarchy(Region.FlagsEdit);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Refresh the hierarchy UI for a specific region.
        /// </summary>
        private void RefreshHierarchy(Region region = Region.All)
        {
            try
            {
                if (Hierarchy.IsInitialized)
                {
                    _ = Hierarchy.Refresh(region);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to refresh hierarchy: {ex.Message}");
            }

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Create a unique duplicate name by appending " (Copy)" suffix.
        /// Based on AbstractHierarchy.CreateDuplicateName()
        /// </summary>
        private string CreateDuplicateName(List<string> existingNames, string originalName)
        {
            var baseName = originalName;
            var copyText = " (Copy)";

            // Remove existing copy suffix
            if (baseName.EndsWith(copyText))
            {
                baseName = baseName.Substring(0, baseName.Length - copyText.Length);
            }

            var newName = baseName + copyText;
            var counter = 1;

            while (existingNames.Contains(newName))
            {
                counter++;
                newName = baseName + copyText + counter;
            }

            return newName;
        }

        #endregion
    }
}
