﻿using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using MEC;
using Mono.Unix.Native;
using Subclass.Effects;
using Subclass.MonoBehaviours;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Subclass
{
    public class Tracking
    {
        public static Dictionary<SubClass, int> SubClassesSpawned = new Dictionary<SubClass, int>();

        public static Dictionary<Player, SubClass> PlayersWithSubclasses = new Dictionary<Player, SubClass>();

        public static Dictionary<Player, Dictionary<AbilityType, float>> Cooldowns = new Dictionary<Player, Dictionary<AbilityType, float>>();
        public static Dictionary<Player, Dictionary<AbilityType, int>> AbilityUses = new Dictionary<Player, Dictionary<AbilityType, int>>();

        public static Dictionary<Player, float> PlayersThatBypassedTeslaGates = new Dictionary<Player, float>();

        public static Dictionary<Player, float> PlayersThatJustGotAClass = new Dictionary<Player, float>();

        public static Dictionary<Player, List<Player>> PlayersWithZombies = new Dictionary<Player, List<Player>>();
        public static Dictionary<Player, List<Player>> PlayersThatHadZombies = new Dictionary<Player, List<Player>>();

        public static Dictionary<Player, RoleType> PreviousRoles = new Dictionary<Player, RoleType>();
        public static Dictionary<Player, SubClass> PreviousSubclasses = new Dictionary<Player, SubClass>();
        // public static Player LastDiedTo035 = null; - I would love to implement this and keep 035 data... but there's no event to listen to for a player dying by picking up 035 :(

        public static List<Player> FriendlyFired = new List<Player>();

        public static List<Player> PlayersInvisibleByCommand = new List<Player>();

        public static List<string> QueuedCassieMessages = new List<string>();

        public static float RoundStartedAt = 0f;

        public static List<Player> NextSpawnWave = new List<Player>();
        public static Dictionary<RoleType, SubClass> NextSpawnWaveGetsRole = new Dictionary<RoleType, SubClass>();
        public static List<SubClass> SpawnWaveSpawns = new List<SubClass>();
        public static Dictionary<SubClass, int> ClassesGiven = new Dictionary<SubClass, int>();
        public static List<SubClass> DontGiveClasses = new List<SubClass>();

        public static Dictionary<Player, string> PreviousBadges = new Dictionary<Player, string>();

        static System.Random rnd = new System.Random();

        public static void AddClass(Player player, SubClass subClass, bool is035 = false, bool lite = false)
        {
            if (is035)
            {
                SubClass copy = new SubClass(subClass);
                if (!copy.Abilities.Contains(AbilityType.Disable096Trigger)) copy.Abilities.Add(AbilityType.Disable096Trigger);
                if (!copy.Abilities.Contains(AbilityType.Disable173Stop)) copy.Abilities.Add(AbilityType.Disable173Stop);
                if (!copy.Abilities.Contains(AbilityType.NoSCPDamage)) copy.Abilities.Add(AbilityType.NoSCPDamage);
                copy.BoolOptions["HasFriendlyFire"] = true;
                copy.BoolOptions["TakesFriendlyFire"] = true;
                copy.SpawnsAs = RoleType.None;
                copy.SpawnLocations.Clear();
                copy.SpawnLocations.Add("Unknown");
                copy.IntOptions["MaxHealth"] = -1;
                copy.IntOptions["HealthOnSpawn"] = -1;
                copy.IntOptions["MaxArmor"] = -1;
                copy.IntOptions["ArmorOnSpawn"] = -1;
                copy.SpawnItems.Clear();
                copy.RolesThatCantDamage.Clear();
                copy.StringOptions["GotClassMessage"] = subClass.StringOptions["GotClassMessage"] + " You are SCP-035.";
                copy.CantDamageRoles = new List<RoleType>();

                subClass = new SubClass(copy.Name + "-SCP-035 (p)", copy.AffectsRoles, copy.StringOptions, copy.BoolOptions, copy.IntOptions,
                    copy.FloatOptions, copy.SpawnLocations, copy.SpawnItems,
                    new Dictionary<AmmoType, int>()
                    {
                        { AmmoType.Nato556, -1 },
                        { AmmoType.Nato762, -1 },
                        { AmmoType.Nato9, -1 }
                    }, copy.Abilities, copy.AbilityCooldowns, copy.AdvancedFFRules, copy.OnHitEffects, copy.OnSpawnEffects, 
                    copy.RolesThatCantDamage, "SCP", RoleType.None, null, subClass.OnDamagedEffects, null
                );
            }
            if (NextSpawnWave.Contains(player) && NextSpawnWaveGetsRole.ContainsKey(player.Role) && !SpawnWaveSpawns.Contains(subClass))
            {
                if (SubClassesSpawned.ContainsKey(subClass)) SubClassesSpawned[subClass]++;
                else SubClassesSpawned.Add(subClass, 1);
                SpawnWaveSpawns.Add(subClass);
            }
            else if (!SpawnWaveSpawns.Contains(subClass))
            {
                if (SubClassesSpawned.ContainsKey(subClass)) SubClassesSpawned[subClass]++;
                else SubClassesSpawned.Add(subClass, 1);
            }
            PlayersWithSubclasses.Add(player, subClass);
            if (!PlayersThatJustGotAClass.ContainsKey(player)) PlayersThatJustGotAClass.Add(player, Time.time + 3f);
            else PlayersThatJustGotAClass[player] = Time.time + 3f;

            int spawnIndex = rnd.Next(subClass.SpawnLocations.Count);

            try
            {
                player.Broadcast(subClass.FloatOptions.ContainsKey("BroadcastTimer") ? (ushort)subClass.FloatOptions["BroadcastTimer"] : (ushort)Subclass.Instance.Config.GlobalBroadcastTime, subClass.StringOptions["GotClassMessage"]);
                if (subClass.StringOptions.ContainsKey("CassieAnnouncement") &&
                    !QueuedCassieMessages.Contains(subClass.StringOptions["CassieAnnouncement"])) QueuedCassieMessages.Add(subClass.StringOptions["CassieAnnouncement"]);

                if (subClass.SpawnsAs != RoleType.None)
                {
                    player.SetRole(subClass.SpawnsAs, subClass.SpawnLocations[spawnIndex] != "Unknown");
                }

                if (subClass.SpawnItems.Count != 0)
                {
                    player.ClearInventory();
                    foreach (var item in subClass.SpawnItems)
                    {
                        foreach (var item2 in item.Value)
                        {
                            if ((rnd.NextDouble() * 100) < subClass.SpawnItems[item.Key][item2.Key])
                            {
                                if (item2.Key == ItemType.None) break;
                                player.AddItem(item2.Key);
                                break;
                            }
                        }
                    }
                }
                if (subClass.IntOptions["MaxHealth"] != -1) player.MaxHealth = subClass.IntOptions["MaxHealth"];
                if (subClass.IntOptions["HealthOnSpawn"] != -1) player.Health = subClass.IntOptions["HealthOnSpawn"];
                if (subClass.IntOptions["MaxArmor"] != -1) player.MaxAdrenalineHealth = subClass.IntOptions["MaxArmor"];
                if (subClass.IntOptions["ArmorOnSpawn"] != -1) player.AdrenalineHealth = subClass.IntOptions["ArmorOnSpawn"];

                Vector3 scale = new Vector3(player.Scale.x, player.Scale.y, player.Scale.z);

                if (subClass.FloatOptions.ContainsKey("ScaleX")) scale.x = subClass.FloatOptions["ScaleX"];
                if (subClass.FloatOptions.ContainsKey("ScaleY")) scale.y = subClass.FloatOptions["ScaleY"];
                if (subClass.FloatOptions.ContainsKey("ScaleZ")) scale.z = subClass.FloatOptions["ScaleZ"];

                player.Scale = scale;

                if (!subClass.BoolOptions["DisregardHasFF"])
                {
                    player.IsFriendlyFireEnabled = subClass.BoolOptions["HasFriendlyFire"];
                }
            }
            catch (KeyNotFoundException e)
            {
                Log.Error($"A required option was not provided. Class: {subClass.Name}");
                throw new Exception($"A required option was not provided. Class: {subClass.Name}");
            }

            if (subClass.StringOptions.ContainsKey("Nickname")) player.DisplayNickname = subClass.StringOptions["Nickname"].Replace("{name}", player.Nickname);

            if (subClass.Abilities.Contains(AbilityType.GodMode)) player.IsGodModeEnabled = true;
            if (subClass.Abilities.Contains(AbilityType.InvisibleUntilInteract)) player.ReferenceHub.playerEffectsController.EnableEffect<Scp268>();
            if (subClass.Abilities.Contains(AbilityType.InfiniteSprint)) player.GameObject.AddComponent<MonoBehaviours.InfiniteSprint>();
            if (subClass.Abilities.Contains(AbilityType.Disable173Stop)) Scp173.TurnedPlayers.Add(player);
            if (subClass.Abilities.Contains(AbilityType.Scp939Vision))
            {
                Visuals939 visuals = player.ReferenceHub.playerEffectsController.GetEffect<Visuals939>();
                visuals.Intensity = 2;
                player.ReferenceHub.playerEffectsController.EnableEffect(visuals);
            }
            if (subClass.Abilities.Contains(AbilityType.NoArmorDecay)) player.ReferenceHub.playerStats.artificialHpDecay = 0f;

            if (subClass.SpawnAmmo[AmmoType.Nato556] != -1)
            {
                player.Ammo[(int)AmmoType.Nato556] = (uint)subClass.SpawnAmmo[AmmoType.Nato556];
            }

            if (subClass.SpawnAmmo[AmmoType.Nato762] != -1)
            {
                player.Ammo[(int)AmmoType.Nato762] = (uint)subClass.SpawnAmmo[AmmoType.Nato762];
            }

            if (subClass.SpawnAmmo[AmmoType.Nato9] != -1)
            {
                player.Ammo[(int)AmmoType.Nato9] = (uint)subClass.SpawnAmmo[AmmoType.Nato9];
            }

            if (subClass.Abilities.Contains(AbilityType.InfiniteAmmo))
            {
                player.Ammo[0] = uint.MaxValue;
                player.Ammo[1] = uint.MaxValue;
                player.Ammo[2] = uint.MaxValue;
            }

            if(subClass.Abilities.Contains(AbilityType.HealAura))
            {
                bool affectSelf = subClass.BoolOptions.ContainsKey("HealAuraAffectsSelf") ? subClass.BoolOptions["HealAuraAffectsSelf"] : true;
                bool affectAllies = subClass.BoolOptions.ContainsKey("HealAuraAffectsAllies") ? subClass.BoolOptions["HealAuraAffectsAllies"] : true;
                bool affectEnemies = subClass.BoolOptions.ContainsKey("HealAuraAffectsEnemies") ? subClass.BoolOptions["HealAuraAffectsEnemies"] : false;

                float healthPerTick = subClass.FloatOptions.ContainsKey("HealAuraHealthPerTick") ? subClass.FloatOptions["HealAuraHealthPerTick"] : 5f;
                float radius = subClass.FloatOptions.ContainsKey("HealAuraRadius") ? subClass.FloatOptions["HealAuraRadius"] : 4f;
                float tickRate = subClass.FloatOptions.ContainsKey("HealAuraTickRate") ? subClass.FloatOptions["HealAuraTickRate"] : 5f;

                player.ReferenceHub.playerEffectsController.AllEffects.Add(typeof(HealAura), new HealAura(player.ReferenceHub, healthPerTick, radius, affectSelf, affectAllies, affectEnemies, tickRate));
                Timing.CallDelayed(0.5f, () =>
                {
                    player.ReferenceHub.playerEffectsController.EnableEffect<HealAura>(float.MaxValue);
                });
            }

            if (subClass.Abilities.Contains(AbilityType.DamageAura))
            {
                bool affectSelf = subClass.BoolOptions.ContainsKey("DamageAuraAffectsSelf") ? subClass.BoolOptions["DamageAuraAffectsSelf"] : false;
                bool affectAllies = subClass.BoolOptions.ContainsKey("DamageAuraAffectsAllies") ? subClass.BoolOptions["DamageAuraAffectsAllies"] : false;
                bool affectEnemies = subClass.BoolOptions.ContainsKey("DamageAuraAffectsEnemies") ? subClass.BoolOptions["DamageAuraAffectsEnemies"] : true;

                float healthPerTick = subClass.FloatOptions.ContainsKey("DamageAuraDamagePerTick") ? subClass.FloatOptions["DamageAuraDamagePerTick"] : 5f;
                float radius = subClass.FloatOptions.ContainsKey("DamageAuraRadius") ? subClass.FloatOptions["DamageAuraRadius"] : 4f;
                float tickRate = subClass.FloatOptions.ContainsKey("DamageAuraTickRate") ? subClass.FloatOptions["DamageAuraTickRate"] : 5f;

                player.ReferenceHub.playerEffectsController.AllEffects.Add(typeof(DamageAura), new DamageAura(player.ReferenceHub, healthPerTick, radius, affectSelf, affectAllies, affectEnemies, tickRate));
                Timing.CallDelayed(0.5f, () =>
                {
                    player.ReferenceHub.playerEffectsController.EnableEffect<DamageAura>(float.MaxValue);
                });
            }

            if (!is035)
            {
                if (player.GlobalBadge?.Type == 0) // Comply with verified server rules.
                {
                    AddPreviousBadge(player, true);
                    if (subClass.StringOptions.ContainsKey("Badge")) player.ReferenceHub.serverRoles.HiddenBadge = subClass.StringOptions["Badge"];
                }
                else
                {
                    AddPreviousBadge(player);
                    if (subClass.StringOptions.ContainsKey("Badge")) player.RankName = subClass.StringOptions["Badge"];
                    if (subClass.StringOptions.ContainsKey("BadgeColor")) player.RankColor = subClass.StringOptions["BadgeColor"];
                }
            }

            if (subClass.OnSpawnEffects.Count != 0)
            {
                Timing.CallDelayed(0.1f, () =>
                {
                    Log.Debug($"Subclass {subClass.Name} has on spawn effects", Subclass.Instance.Config.Debug);
                    foreach (string effect in subClass.OnSpawnEffects)
                    {
                        Log.Debug($"Evaluating chance for on spawn {effect} for player {player.Nickname}", Subclass.Instance.Config.Debug);
                        if(!subClass.FloatOptions.ContainsKey(("OnSpawn" + effect + "Chance")))
                        {
                            Log.Error($"ERROR! Spawn effect {effect} chance not found! Please make sure to add this to your float options");
                            continue;
                        }
                        if ((rnd.NextDouble() * 100) < subClass.FloatOptions[("OnSpawn" + effect + "Chance")])
                        {
                            player.ReferenceHub.playerEffectsController.EnableByString(effect,
                                subClass.FloatOptions.ContainsKey(("OnSpawn" + effect + "Duration")) ?
                                subClass.FloatOptions[("OnSpawn" + effect + "Duration")] : -1, true);
                            Log.Debug($"Player {player.Nickname} has been given effect {effect} on spawn", Subclass.Instance.Config.Debug);
                        }
                        else
                        {
                            Log.Debug($"Player {player.Nickname} has been not given effect {effect} on spawn", Subclass.Instance.Config.Debug);
                        }
                    }
                });
            }
            else
            {
                Log.Debug($"Subclass {subClass.Name} has no on spawn effects", Subclass.Instance.Config.Debug);
            }

            if (!lite && subClass.SpawnLocations[spawnIndex] != "Unknown")
            {
                List<Vector3> spawnLocations = new List<Vector3>();
                if (subClass.SpawnLocations[spawnIndex] == "Lcz173Armory") 
                {
                    Door door = GameObject.FindObjectsOfType<Door>().FirstOrDefault((Door dr) => dr.DoorName.ToUpper() == "173_ARMORY");
                    spawnLocations.Add(door.transform.position + (Vector3.right * 2));
                }
                else if (subClass.SpawnLocations[spawnIndex] == "Lcz173")
                {
                    Door door = GameObject.FindObjectsOfType<Door>().FirstOrDefault((Door dr) => dr.DoorName.ToUpper() == "173");
                    spawnLocations.Add(door.transform.position);
                }
                else if (subClass.SpawnLocations[spawnIndex] == "Lcz173Bottom")
                {
                    Door door = GameObject.FindObjectsOfType<Door>().FirstOrDefault((Door dr) => dr.DoorName.ToUpper() == "173_BOTTOM");
                    spawnLocations.Add(door.transform.position);
                }
                else spawnLocations = Map.Rooms.Where(r => r.Type.ToString() == subClass.SpawnLocations[spawnIndex]).Select(r => r.Transform.position).ToList();
                if (spawnLocations.Count != 0)
                {
                    Timing.CallDelayed(0.3f, () =>
                    {
                        Vector3 offset = new Vector3(0, 1f, 0);
                        if (subClass.FloatOptions.ContainsKey("SpawnOffsetX")) offset.x = subClass.FloatOptions["SpawnOffsetX"];
                        if (subClass.FloatOptions.ContainsKey("SpawnOffsetY")) offset.y = subClass.FloatOptions["SpawnOffsetY"];
                        if (subClass.FloatOptions.ContainsKey("SpawnOffsetZ")) offset.z = subClass.FloatOptions["SpawnOffsetZ"];
                        Vector3 pos = spawnLocations[rnd.Next(spawnLocations.Count)];
                        PlayerMovementSync.FindSafePosition(pos + offset, out pos, true);
                        player.ReferenceHub.playerMovementSync.OverridePosition(pos, 0f);
                    });
                }
            }

            if (subClass.IntOptions.ContainsKey("MaxPerSpawnWave"))
            {
                if (!ClassesGiven.ContainsKey(subClass))
                {
                    ClassesGiven.Add(subClass, 1);
                    Timing.CallDelayed(5f, () =>
                    {
                        DontGiveClasses.Clear();
                        ClassesGiven.Clear();
                    });
                }
                else ClassesGiven[subClass]++;
                if (ClassesGiven[subClass] >= subClass.IntOptions["MaxPerSpawnWave"])
                {
                    if (!DontGiveClasses.Contains(subClass))
                    {
                        DontGiveClasses.Add(subClass);
                    }
                }
            }

            if (player.Role != RoleType.ClassD && player.Role != RoleType.Scientist && (subClass.EscapesAs[0] != RoleType.None || subClass.EscapesAs[1] != RoleType.None))
            {
                player.GameObject.AddComponent<EscapeBehaviour>();

                EscapeBehaviour eb = player.GameObject.GetComponent<EscapeBehaviour>();
                eb.EscapesAsNotCuffed = subClass.EscapesAs[0];
                eb.EscapesAsCuffed = subClass.EscapesAs[1];
            }
            Log.Debug($"Player with name {player.Nickname} got subclass {subClass.Name}", Subclass.Instance.Config.Debug);
        }

        public static void RemoveAndAddRoles(Player p, bool dontAddRoles = false, bool is035 = false, bool escaped = false)
        {
            if (PlayersThatJustGotAClass.ContainsKey(p) && PlayersThatJustGotAClass[p] > Time.time) return;
            if (RoundJustStarted()) return;
            if (PlayersInvisibleByCommand.Contains(p)) PlayersInvisibleByCommand.Remove(p);
            if (Cooldowns.ContainsKey(p)) Cooldowns.Remove(p);
            if (FriendlyFired.Contains(p)) FriendlyFired.RemoveAll(e => e == p);
            if (PlayersWithSubclasses.ContainsKey(p) && PlayersWithSubclasses[p].Abilities.Contains(AbilityType.Disable173Stop)
                && Scp173.TurnedPlayers.Contains(p)) Scp173.TurnedPlayers.Remove(p);
            if (PlayersWithSubclasses.ContainsKey(p) && PlayersWithSubclasses[p].Abilities.Contains(AbilityType.NoArmorDecay))
                p.ReferenceHub.playerStats.artificialHpDecay = 0.75f;
            //if (PlayersWithZombies.ContainsKey(p) && escaped)
            //{
            //    PlayersThatHadZombies.Add(p, PlayersWithZombies[p]);
            //    foreach (Player z in PlayersThatHadZombies[p])
            //    {
            //        z.GameObject.AddComponent<EscapeBehaviour>();

            //        RoleType r = RoleType.None;

            //        z.GameObject.GetComponent<EscapeBehaviour>().EscapesAs = r;
            //    }
            //    PlayersWithZombies.Remove(p);
            //}

            if (p.ReferenceHub.serverRoles.HiddenBadge != null && p.ReferenceHub.serverRoles.HiddenBadge != "") p.ReferenceHub.serverRoles.HiddenBadge = null;


            SubClass subClass = PlayersWithSubclasses.ContainsKey(p) ? PlayersWithSubclasses[p] : null;

            if (subClass != null)
            {
                if (!PreviousSubclasses.ContainsKey(p)) PreviousSubclasses.Add(p, subClass);
                else PreviousSubclasses[p] = subClass;

                if (PreviousBadges.ContainsKey(p))
                {
                    if (subClass.StringOptions.ContainsKey("Badge") && p.RankName == subClass.StringOptions["Badge"])
                    {
                        p.RankName = PreviousBadges.ContainsKey(p) ? System.Text.RegularExpressions.Regex.Split(PreviousBadges[p], System.Text.RegularExpressions.Regex.Escape(" [-/-] "))[0] : null;
                        p.RankColor = PreviousBadges.ContainsKey(p) ? System.Text.RegularExpressions.Regex.Split(PreviousBadges[p], System.Text.RegularExpressions.Regex.Escape(" [-/-] "))[1] : null;
                    }
                    else if (subClass.StringOptions.ContainsKey("Badge") && p.ReferenceHub.serverRoles.HiddenBadge == subClass.StringOptions["Badge"])
                    {
                        p.ReferenceHub.serverRoles.HiddenBadge = PreviousBadges.ContainsKey(p) ? System.Text.RegularExpressions.Regex.Split(PreviousBadges[p], System.Text.RegularExpressions.Regex.Escape(" [-/-] "))[0] : null;
                    }
                }

                if (subClass.StringOptions.ContainsKey("Nickname")) p.DisplayNickname = null;

                if (subClass.Abilities.Contains(AbilityType.HealAura))
                {
                    p.ReferenceHub.playerEffectsController.DisableEffect<HealAura>();
                    p.ReferenceHub.playerEffectsController.AllEffects.Remove(typeof(HealAura));
                }

                if (subClass.Abilities.Contains(AbilityType.DamageAura))
                {
                    p.ReferenceHub.playerEffectsController.DisableEffect<DamageAura>();
                    p.ReferenceHub.playerEffectsController.AllEffects.Remove(typeof(DamageAura));
                }
            }

            if (p.GameObject?.GetComponent<InfiniteSprint>() != null)
            {
                Log.Debug($"Player {p.Nickname} has infinite stamina, destroying", Subclass.Instance.Config.Debug);
                p.GameObject.GetComponent<InfiniteSprint>().Destroy();
                p.IsUsingStamina = true; // Have to set it to true for it to remove fully... for some reason?
            }

            if (p.GameObject?.GetComponent<EscapeBehaviour>() != null)
            {
                Log.Debug($"Player {p.Nickname} has escapebehaviour, destroying", Subclass.Instance.Config.Debug);
                p.GameObject.GetComponent<EscapeBehaviour>().Destroy();
            }

            if (PlayersWithSubclasses.ContainsKey(p)) PlayersWithSubclasses.Remove(p);
            if (escaped)
            {
                if (!PlayersThatJustGotAClass.ContainsKey(p)) PlayersThatJustGotAClass.Add(p, Time.time + 3f);
                else PlayersThatJustGotAClass[p] = Time.time + 3f;
            }
            if (!dontAddRoles) Subclass.Instance.server.MaybeAddRoles(p, is035, escaped);
        }

        public static void AddToFF(Player p)
        {
            if (!FriendlyFired.Contains(p)) FriendlyFired.Add(p);
        }

        public static void TryToRemoveFromFF(Player p)
        {
            if (FriendlyFired.Contains(p)) FriendlyFired.Remove(p);
        }

        public static void AddCooldown(Player p, AbilityType ability)
        {
            try
            {
                if (!Cooldowns.ContainsKey(p)) Cooldowns.Add(p, new Dictionary<AbilityType, float>());
                Cooldowns[p][ability] = Time.time;
            }catch(KeyNotFoundException e)
            {
                throw new Exception($"You are missing an ability cooldown that MUST have a cooldown. Make sure to add {ability} to your ability cooldowns.", e);
            }
        }

        public static void UseAbility(Player p, AbilityType ability, SubClass subClass)
        {
            if (!subClass.IntOptions.ContainsKey(ability.ToString() + "MaxUses")) return;
            if (!AbilityUses.ContainsKey(p)) AbilityUses.Add(p, new Dictionary<AbilityType, int>());
            if (!AbilityUses[p].ContainsKey(ability)) AbilityUses[p].Add(ability, 0);
            AbilityUses[p][ability]++;
        }

        public static bool CanUseAbility(Player p, AbilityType ability, SubClass subClass)
        {
            if (!AbilityUses.ContainsKey(p) || !AbilityUses[p].ContainsKey(ability) || !subClass.IntOptions.ContainsKey(ability.ToString() + "MaxUses") ||
                AbilityUses[p][ability] < subClass.IntOptions[(ability.ToString() + "MaxUses")]) return true;
            return false;
        }

        public static void DisplayCantUseAbility(Player p, AbilityType ability, SubClass subClass, string abilityName)
        {
            p.ClearBroadcasts();
            p.Broadcast(4, subClass.StringOptions["OutOfAbilityUses"].Replace("{ability}", abilityName));
        }

        public static bool OnCooldown(Player p, AbilityType ability, SubClass subClass)
        {
            return Cooldowns.ContainsKey(p) && Cooldowns[p].ContainsKey(ability)
                && Time.time - Cooldowns[p][ability] < subClass.AbilityCooldowns[ability];
        }

        public static float TimeLeftOnCooldown(Player p, AbilityType ability, SubClass subClass, float time)
        {
            if (Cooldowns.ContainsKey(p) && Cooldowns[p].ContainsKey(ability))
            {
                return subClass.AbilityCooldowns[ability] - (time - Cooldowns[p][ability]);
            }
            return 0;
        }

        public static void DisplayCooldown(Player p, AbilityType ability, SubClass subClass, string abilityName, float time)
        {
            float timeLeft = TimeLeftOnCooldown(p, ability, subClass, time);
            p.ClearBroadcasts();
            p.Broadcast((ushort)Mathf.Clamp(timeLeft - timeLeft / 4, 0.5f, 3), subClass.StringOptions["AbilityCooldownMessage"].Replace("{ability}", abilityName).Replace("{seconds}", timeLeft.ToString()));
        }

        public static bool PlayerJustBypassedTeslaGate(Player p)
        {
            return PlayersThatBypassedTeslaGates.ContainsKey(p) && Time.time - PlayersThatBypassedTeslaGates[p] < 3f;
        }

        public static bool RoundJustStarted()
        {
            return Time.time - RoundStartedAt < 5f;
        }

        public static void AddPreviousTeam(Player p)
        {
            if (PreviousRoles.ContainsKey(p)) PreviousRoles[p] = p.Role;
            else PreviousRoles.Add(p, p.Role);
        }

        public static Nullable<RoleType> GetPreviousRole(Player p)
        {
            if (PreviousRoles.ContainsKey(p)) return PreviousRoles[p];
            return null;
        }

        public static Nullable<Team> GetPreviousTeam(Player p)
        {
            if (PreviousRoles.ContainsKey(p)) return PreviousRoles[p].GetTeam();
            return null;
        }

        public static void AddZombie(Player p, Player z)
        {
            if (!PlayersWithZombies.ContainsKey(p)) PlayersWithZombies.Add(p, new List<Player>());
            PlayersWithZombies[p].Add(z);
        }

        public static void RemoveZombie(Player p)
        {
            List<Player> toRemoveWith = new List<Player>();
            List<Player> toRemoveHad = new List<Player>();
            foreach (var item in PlayersWithZombies)
            {
                if (item.Value.Contains(p)) item.Value.Remove(p);
                if (item.Value.Count == 0) toRemoveWith.Add(item.Key);
            }
            foreach (var item in PlayersThatHadZombies)
            {
                if (item.Value.Contains(p)) item.Value.Remove(p);
                if (item.Value.Count == 0) toRemoveHad.Add(item.Key);
            }

            foreach (Player p1 in toRemoveWith) PlayersWithZombies.Remove(p1);
            foreach (Player p1 in toRemoveHad) PlayersThatHadZombies.Remove(p1);
        }

        public static bool PlayerHasFFToPlayer(Player attacker, Player target)
        {
            Log.Debug($"Checking FF rules for Attacker: {attacker.Nickname} Target: {target?.Nickname}", Subclass.Instance.Config.Debug);
            if (target != null)
            {
                Log.Debug($"Checking zombies", Subclass.Instance.Config.Debug);
                if (PlayersWithZombies.Where(p => p.Value.Contains(target)).Count() > 0)
                {
                    return true;
                }

                Log.Debug($"Checking classes", Subclass.Instance.Config.Debug);
                if (PlayersWithSubclasses.ContainsKey(attacker) && PlayersWithSubclasses.ContainsKey(target) &&
                    PlayersWithSubclasses[attacker].AdvancedFFRules.Contains(PlayersWithSubclasses[target].Name))
                {
                    return true;
                }

                Log.Debug($"Checking FF rules in classes", Subclass.Instance.Config.Debug);
                if (FriendlyFired.Contains(target) || 
                    (PlayersWithSubclasses.ContainsKey(attacker) &&
                    !PlayersWithSubclasses[attacker].BoolOptions["DisregardHasFF"] && PlayersWithSubclasses[attacker].BoolOptions["HasFriendlyFire"]) ||
                    (PlayersWithSubclasses.ContainsKey(target) && !PlayersWithSubclasses[target].BoolOptions["DisregardTakesFF"] &&
                    PlayersWithSubclasses[target].BoolOptions["TakesFriendlyFire"]))
                {
                    if (!FriendlyFired.Contains(target) && !(PlayersWithSubclasses.ContainsKey(target) && PlayersWithSubclasses[target].BoolOptions["TakesFriendlyFire"]))
                        AddToFF(attacker);
                    return true;
                }
                else
                {
                    Log.Debug($"Checking takes friendly fire", Subclass.Instance.Config.Debug);
                    if (PlayersWithSubclasses.ContainsKey(target) && !PlayersWithSubclasses[target].BoolOptions["DisregardTakesFF"] &&
                    !PlayersWithSubclasses[target].BoolOptions["TakesFriendlyFire"])
                    {
                        return false;
                    }
                }
            }
            return false;
        }

        public static bool AllowedToDamage(Player t, Player a)
        {
            Log.Debug($"Checking allowed damage rules for Attacker: {a.Nickname} to target role: {t.Role}", Subclass.Instance.Config.Debug);
            if (a.Id == t.Id) return true;
            if (PlayersWithSubclasses.ContainsKey(a)) return !PlayersWithSubclasses[a].CantDamageRoles.Contains(t.Role);
            if (PlayersWithSubclasses.ContainsKey(t)) return !PlayersWithSubclasses[t].RolesThatCantDamage.Contains(a.Role);
            return true;
        }

        public static void CheckRoundEnd()
        {
            Log.Debug("Checking round end", Subclass.Instance.Config.Debug);
            List<string> teamsAlive = Player.List.Select(p1 => p1.Team.ToString()).ToList();
            teamsAlive.RemoveAll(t => t == "RIP");
            foreach (var item in PlayersWithSubclasses.Where(s => s.Value.EndsRoundWith != "RIP"))
            {
                teamsAlive.Remove(item.Key.Team.ToString());
                teamsAlive.Add(item.Value.EndsRoundWith);
                Log.Debug($"SubClass doesn't end with normal team, ends with: {item.Value.EndsRoundWith}", Subclass.Instance.Config.Debug);
            }

            for (int i = 0; i < teamsAlive.Count; i++)
            {
                string t = teamsAlive[i];
                if (t == "CDP")
                {
                    teamsAlive.RemoveAt(i);
                    teamsAlive.Insert(i, "CHI");
                }
                else if (t == "RSC")
                {
                    teamsAlive.RemoveAt(i);
                    teamsAlive.Insert(i, "MTF");
                }
                else if (t == "TUT")
                {
                    teamsAlive.RemoveAt(i);
                    teamsAlive.Insert(i, "MTF");
                }
            }

            List<string> uniqueTeamsAlive = new List<string>();

            foreach(string t in teamsAlive)
            {
                if (!uniqueTeamsAlive.Contains(t)) uniqueTeamsAlive.Add(t);
            }

            Log.Debug($"Number of unique teams alive: {uniqueTeamsAlive.Count}. Contains ALL? {uniqueTeamsAlive.Contains("ALL")}", Subclass.Instance.Config.Debug);

            if (uniqueTeamsAlive.Count == 2 && uniqueTeamsAlive.Contains("SCP"))
            {
                List<Player> zombies = API.RevivedZombies();
                if (Player.List.Where(p => p.Team == Team.SCP).All(p => zombies.Contains(p)))
                {
                    foreach (Player zombie in zombies) zombie.Kill();
                }
            }

            if (uniqueTeamsAlive.Count == 2 && uniqueTeamsAlive.Contains("ALL"))
            {
                string team = uniqueTeamsAlive.Find(t => t != "ALL");
                foreach (var item in PlayersWithSubclasses)
                {
                    PlayersThatJustGotAClass[item.Key] += 3;
                    if (team == "MTF") item.Key.SetRole(RoleType.NtfScientist, true);
                    else if (team == "CHI") item.Key.SetRole(RoleType.ChaosInsurgency, true);
                    else item.Key.SetRole(RoleType.Scp0492, true);
                }
            }

            if (uniqueTeamsAlive.Count == 1)
            {
                foreach (var item in PlayersWithSubclasses.Where(t => t.Value.EndsRoundWith != "RIP"))
                {
                    PlayersThatJustGotAClass[item.Key] += 3;
                    if (uniqueTeamsAlive[0] == "MTF") item.Key.SetRole(RoleType.NtfScientist, true);
                    else if (uniqueTeamsAlive[0] == "CHI") item.Key.SetRole(RoleType.ChaosInsurgency, true);
                    else item.Key.SetRole(RoleType.Scp0492, true);
                }
            }

            if (PlayersWithSubclasses.Count(s => s.Value.EndsRoundWith != "RIP") > 0)
            {
                foreach (Player player in PlayersWithSubclasses.Keys)
                {
                    if (PlayersWithSubclasses[player].EndsRoundWith != "RIP" &&
                        PlayersWithSubclasses[player].EndsRoundWith != "ALL" &&
                        PlayersWithSubclasses[player].EndsRoundWith != player.Team.ToString() &&
                        teamsAlive.Count(e => e == PlayersWithSubclasses[player].EndsRoundWith) == 1)
                    {
                        PlayersThatJustGotAClass[player] += 3;
                        if (PlayersWithSubclasses[player].EndsRoundWith == "MTF") player.SetRole(RoleType.NtfScientist, true);
                        else if (PlayersWithSubclasses[player].EndsRoundWith == "CHI") player.SetRole(RoleType.ChaosInsurgency, true);
                        else player.SetRole(RoleType.Scp0492, true);
                    }
                }
            }
        }

        public static int ClassesSpawned(SubClass subClass)
        {
            if (!SubClassesSpawned.ContainsKey(subClass)) return 0;
            return SubClassesSpawned[subClass];
        }

        public static void AddPreviousBadge(Player p, bool hidden = false)
        {
            if (hidden)
            {
                if (PreviousBadges.ContainsKey(p)) PreviousBadges[p] = p.ReferenceHub.serverRoles.HiddenBadge + " [-/-] ";
                else PreviousBadges.Add(p, p.ReferenceHub.serverRoles.HiddenBadge + " [-/-] ");
            }else
            {
                if (PreviousBadges.ContainsKey(p)) PreviousBadges[p] = p.RankName + " [-/-] " + p.RankColor;
                else PreviousBadges.Add(p, p.RankName + " [-/-] " + p.RankColor);
            }
        }
    }
}
