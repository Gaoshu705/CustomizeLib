using BepInEx;
using BepInEx.Unity.IL2CPP;
using CustomizeLib.BepInEx;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System.Reflection;
using UnityEngine;

namespace GoldImitater.BepInEx
{
    [BepInPlugin("salmon.goldimitater", "GoldImitater", "1.0")]
    public class Core : BasePlugin//960
    {
        public override void Load()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            ClassInjector.RegisterTypeInIl2Cpp<GoldImitater>(); Console.OutputEncoding = System.Text.Encoding.UTF8;
            var ab = CustomCore.GetAssetBundle(Assembly.GetExecutingAssembly(), "goldimitater");
            CustomCore.RegisterCustomPlant<Imitater, GoldImitater>(GoldImitater.PlantID, ab.GetAsset<GameObject>("GoldImitaterPrefab"),
                ab.GetAsset<GameObject>("GoldImitaterPreview"), [], 0f, 0f, 0, 300, 15, 50);
            CustomCore.AddPlantAlmanacStrings(GoldImitater.PlantID, $"黄金模仿者({GoldImitater.PlantID})",
                "或许是宝藏呢？\n\n<color=#3D1400>贴图作者：@林秋-AutumnLin</color>\n<color=#3D1400>特点：</color><color=red>短时间内变身随机召唤植物或僵尸。</color>\n\n<color=#3D1400>“茄本无相，吾有万象。”黄金模仿者侃侃而谈：“就像你看到的，我可以是任何植物，任何僵尸，在后院奋战的豌豆是我，高举旗帜冲锋的僵尸也是我，沉入土壤的尸体是我，哇哇</color>\n花费：<color=red>50</color>\n冷却时间：<color=red>15秒</color>\n<color=#3D1400>啼哭的孩童也是我。但是'我'与'我'之间的性格是不同的，记忆是不同的，童年成长都是不同的，他们各司其职，他们努力生活。“那拥有不同记忆不同性格的你还是你么”黄金模仿者似乎陷入了沉思……不再说话。</color>\n\n\n\n\n\n花费：<color=red>50</color>\n冷却时间：<color=red>15秒</color>");
            CustomCore.RegisterCustomCardToColorfulCards((PlantType)GoldImitater.PlantID);
        }
    }

    public class GoldImitater : MonoBehaviour
    {
        public static int PlantID = 1931;

        public void AnimSpawn()
        {
            ParticleManager.Instance.SetParticle(ParticleType.RandomCloud, plant.axis.transform.position, plant.thePlantRow);
            try
            {
                int row = plant.thePlantRow;
                int column = plant.thePlantColumn;
                var axis = plant.axis;
                plant.Die();
                var v = UnityEngine.Random.Range(1, 101);
                if (v <= 44)
                {
                    var list = GameAPP.resourcesManager.allPlants.ToArray().ToList().Where(x => x != PlantType.Nothing && x != PlantType.PresentZombie &&
                            x != PlantType.GoldHypnoDoom && x != PlantType.MagnetBox &&
                            x != PlantType.MagnetInterface && x != PlantType.Pit && x != PlantType.Refrash && x != PlantType.Extract_single &&
                            x != PlantType.Extract_ten && !Lawnf.IsUltiPlant(x)).ToList();
                    var type = list[UnityEngine.Random.Range(0, list.Count)];
                    CreatePlant.Instance.SetPlant(column, row, type, isFreeSet: true);
                }
                else if (v <= 64)
                {
                    var list = new List<PlantType>();
                    foreach (var item in GameAPP.resourcesManager.allPlants)
                        if (Lawnf.IsUltiPlant(item) && item != PlantType.GoldHypnoDoom && item != PlantType.Nothing && item != PlantType.PresentZombie &&
                            item != PlantType.GoldHypnoDoom && item != PlantType.MagnetBox &&
                            item != PlantType.MagnetInterface && item != PlantType.Pit && item != PlantType.Refrash && item != PlantType.Extract_single &&
                            item != PlantType.Extract_ten)
                            list.Add(item);
                    CreatePlant.Instance.SetPlant(column, row, list[UnityEngine.Random.Range(0, list.Count)], isFreeSet: true);
                }
                else if (v <= 84)
                {
                    var list = GameAPP.resourcesManager.allZombieTypes.ToArray().ToList().Where(x => x != ZombieType.Nothing && x != ZombieType.ZombieBoss2 &&
                            x != ZombieType.UltimateSnowZombie && x != ZombieType.TrainingDummy && !TypeMgr.IsBossZombie(x)).ToList();
                    var type = list[UnityEngine.Random.Range(0, list.Count)];
                    if (type == ZombieType.ZombieBoss)
                    {
                        var boss = CreateZombie.Instance.SetZombie(0, type, axis.transform.position.x);
                        boss.GetComponent<Zombie>().theHealth *= 10;
                        boss.GetComponent<Zombie>().theMaxHealth *= 10;
                        boss.GetComponent<Zombie>().UpdateHealthText();
                        GameAPP.Instance.PlayMusic(MusicType.Boss);
                    }
                    else
                        CreateZombie.Instance.SetZombie(row, type, axis.transform.position.x);
                }
                else if (v <= 94)
                {
                    var list = new List<ZombieType>();
                    foreach (var item in GameAPP.resourcesManager.allZombieTypes)
                    {
                        if (TypeMgr.UltimateZombie(item) && !TypeMgr.IsBossZombie(item) && item != ZombieType.Nothing && item != ZombieType.ZombieBoss2 &&
                            item != ZombieType.UltimateSnowZombie && item != ZombieType.TrainingDummy)
                            list.Add(item);
                    }
                    var type = list[UnityEngine.Random.Range(0, list.Count)];
                    if (type == ZombieType.ZombieBoss)
                    {
                        var boss = CreateZombie.Instance.SetZombie(0, type, axis.transform.position.x);
                        boss.GetComponent<Zombie>().theHealth *= 10;
                        boss.GetComponent<Zombie>().theMaxHealth *= 10;
                        boss.GetComponent<Zombie>().UpdateHealthText();
                        GameAPP.Instance.PlayMusic(MusicType.Boss);
                    }
                    else
                        CreateZombie.Instance.SetZombie(row, type, axis.transform.position.x);
                }
                else if (v <= 99)
                {
                    var list = new List<ZombieType>();
                    foreach (var item in GameAPP.resourcesManager.allZombieTypes)
                    {
                        if (TypeMgr.IsBossZombie(item) && item != ZombieType.Nothing && item != ZombieType.ZombieBoss2 &&
                            item != ZombieType.UltimateSnowZombie && item != ZombieType.TrainingDummy)
                            list.Add(item);
                    }
                    var type = list[UnityEngine.Random.Range(0, list.Count)];
                    if (type == ZombieType.ZombieBoss)
                    {
                        var boss = CreateZombie.Instance.SetZombie(0, type, axis.transform.position.x);
                        boss.GetComponent<Zombie>().theHealth *= 10;
                        boss.GetComponent<Zombie>().theMaxHealth *= 10;
                        boss.GetComponent<Zombie>().UpdateHealthText();
                        GameAPP.Instance.PlayMusic(MusicType.Boss);
                    }
                    else
                        CreateZombie.Instance.SetZombie(row, type, axis.transform.position.x);
                }
                else
                {
                    if (TravelMgr.Instance is null)
                        return;
                    var list = new List<int>();
                    for (int i = 0; i < TravelMgr.Instance.advancedUpgrades.Count; i++)
                        if (!TravelMgr.Instance.advancedUpgrades[i])
                            list.Add(i);
                    CreatePlant.Instance.SetPlant(column, row, (PlantType)PlantID, isFreeSet: true);
                    TravelMgr.Instance.advancedUpgrades[list[UnityEngine.Random.Range(0, list.Count)]] = true;
                    InGameText.Instance.ShowText("窝给你抽个词条", 3f);
                }
            }
            catch (Exception) { }
        }

        public Imitater plant => gameObject.GetComponent<Imitater>();
    }

    [HarmonyPatch(typeof(CardUI), nameof(CardUI.Start))]
    public class CardUI_Start_Patch
    {
        public static int loopCount = 0;
        [HarmonyPrefix]
        public static void Postfix(CardUI __instance)
        {
            if (__instance.thePlantType == (PlantType)GoldImitater.PlantID && loopCount < 14)
            {
                GameObject go = GameObject.Instantiate(__instance.gameObject, __instance.transform.parent);
                go.transform.position = __instance.transform.position;
                __instance.CD = __instance.fullCD;
                go.GetComponent<CardUI>().CD = go.GetComponent<CardUI>().fullCD;
                loopCount++;
            }
        }
    }

    [HarmonyPatch(typeof(Board), nameof(Board.Start))]
    public class Board_Start_Patch
    {
        [HarmonyPostfix]
        public static void Postfix() => CardUI_Start_Patch.loopCount = 0;
    }
}
