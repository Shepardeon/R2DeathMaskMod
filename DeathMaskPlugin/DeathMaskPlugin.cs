using BepInEx;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Shep.Utils;
using R2API.Utils;
using System.Linq;

namespace Shep.DeathMask
{
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class DeathMaskPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Shepardeon";
        public const string PluginName = "DeathMaskPlugin";
        public const string PluginVersion = "1.0.0";

        private static ItemDef myItemDef;
        private static readonly SphereSearch curseOnKillSphereSearch = new SphereSearch();

        public void Awake()
        {
            Log.Init(Logger);

            myItemDef = ScriptableObject.CreateInstance<ItemDef>();

            myItemDef.name = "SHEP_DEATHMASK_NAME";
            myItemDef.nameToken = "SHEP_DEATHMASK_NAME";
            myItemDef.pickupToken = "SHEP_DEATHMASK_PICKUP";
            myItemDef.descriptionToken = "SHEP_DEATHMASK_DESC";
            myItemDef.loreToken = "SHEP_DEATHMASK_LORE";

            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion();
            myItemDef.pickupIconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
            myItemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
            myItemDef.canRemove = true;
            myItemDef.hidden = false;

            var displayRules = new ItemDisplayRuleDict(null);

            ItemAPI.Add(new CustomItem(myItemDef, displayRules));

            GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;
            Log.Info("Plugin chargé");
        }

        private void GlobalEventManager_onCharacterDeathGlobal(DamageReport report)
        {
            // If a character was killed by the world, we shouldn't do anything.
            if (!report.attacker || !report.attackerBody)
            {
                return;
            }

            var attackerCharacterBody = report.attackerBody;

            // We need an inventory to do check for our item
            if (attackerCharacterBody.inventory)
            {
                // Store the amount of our item we have
                var itemCount = attackerCharacterBody.inventory.GetItemCount(myItemDef.itemIndex);
                if (itemCount > 0)
                {
                    float radius = 8 + 4 * itemCount + report.victimBody.radius;
                    curseOnKillSphereSearch.origin = report.victimBody.corePosition;
                    curseOnKillSphereSearch.mask = LayerIndex.entityPrecise.mask;
                    curseOnKillSphereSearch.radius = radius;
                    curseOnKillSphereSearch.RefreshCandidates();
                    curseOnKillSphereSearch.FilterCandidatesByHurtBoxTeam(TeamMask.GetUnprotectedTeams(report.attackerTeamIndex));
                    curseOnKillSphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
                    var hurtBoxes = curseOnKillSphereSearch.GetHurtBoxes();

                    hurtBoxes.ForEachTry(h =>
                    {
                        if (h.healthComponent)
                        {
                            var dmg = new DamageInfo
                            {
                                inflictor = report.damageInfo.inflictor,
                                attacker = report.damageInfo.attacker,
                                damage = h.healthComponent.fullHealth,
                                damageType = report.damageInfo.damageType
                            };

                            
                            float mul = Mathf.Min(1, itemCount / 4.0f);
                            float thresh = h.healthComponent.fullHealth * mul;

                            Log.Info($"Threshold: {thresh}");

                            // Kills only if below threshold
                            if (h.healthComponent.health <= thresh)
                            {
                                h.healthComponent.TakeDamage(dmg);
                            }
                            // Curses otherwise
                            else
                            {
                                var comp = h.healthComponent.GetComponent<CharacterBody>();
                                comp.AddTimedBuff(RoR2Content.Buffs.DeathMark, 7 * itemCount);
                            }
                        }
                    });
                    EffectManager.SpawnEffect(GlobalEventManager.CommonAssets.igniteOnKillExplosionEffectPrefab, new EffectData
                    {
                        origin = report.victimBody.corePosition,
                        scale = radius,
                        rotation = Util.QuaternionSafeLookRotation(report.damageInfo.force),
                        color = Color.green
                    }, true);
                }
            }
        }

        // The Update() method is run on every frame of the game.
        private void Update()
        {
            // This if statement checks if the player has currently pressed F2.
            if (Input.GetKeyDown(KeyCode.F2))
            {
                // Get the player body to use a position:
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;

                // And then drop our defined item in front of the player.
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(myItemDef.itemIndex), transform.position, transform.forward * 20f);
            }
        }
    }
}
