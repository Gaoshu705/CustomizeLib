using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using BepInEx;
using UnityEngine;
using BepInEx.Unity.IL2CPP;
using System.Reflection;
using Unity.VisualScripting;
using CustomizeLib.BepInEx;

namespace HypnoSuperThreeGatling.BepInEx
{
    [BepInPlugin("salmon.hypnosuperthreegatling", "HypnoSuperThreeGatling", "1.0")]
    public class Core : BasePlugin
    {
        public override void Load()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            ClassInjector.RegisterTypeInIl2Cpp<HypnoSuperThreeGatling>();
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var ab = CustomCore.GetAssetBundle(Assembly.GetExecutingAssembly(), "hypnosuperthreegatling");
            CustomCore.RegisterCustomPlant<SuperThreeGatling, HypnoSuperThreeGatling>(HypnoSuperThreeGatling.PlantID, ab.GetAsset<GameObject>("HypnoSuperThreeGatlingPrefab"),
                ab.GetAsset<GameObject>("HypnoSuperThreeGatlingPreview"), new List<(int, int)>
                {
                    ((int)PlantType.SuperThreeGatling, (int)PlantType.HypnoShroom),
                    ((int)PlantType.SuperHypnoGatling, (int)PlantType.ThreePeater)
                }, 1.5f, 0f, 30, 300, 0f, 950);
            CustomCore.TypeMgrExtra.IsIcePlant.Add((PlantType)HypnoSuperThreeGatling.PlantID);
            CustomCore.AddPlantAlmanacStrings(HypnoSuperThreeGatling.PlantID, $"魅惑三线超级机枪射手({HypnoSuperThreeGatling.PlantID})",
                "向三行发射魅惑豌豆的超级机枪射手。\n\n<color=#3D1400>贴图作者：@林秋-AutumnLin</color>\n<color=#3D1400>伤害：</color><color=red>(30x6)x3/1.5秒</color>\n<color=#3D1400>特点：</color><color=red>每次攻击有2%概率触发大招，5秒内，每0.02秒向三行各发射1个伤害为90的魅惑豌豆。</color>\n<color=#3D1400>融合配方：</color><color=red>超级机枪射手（底座）+三线射手+魅惑菇</color>\n\n<color=#3D1400> “你问我什么是时髦？什么是潮流？”三线超级魅惑机枪射手梳了下头发，“我们行走在潮流的前列，从小到大，我们就是时髦的代名词。”三线超级魅惑机枪射手非常喜欢照镜子，他们能在镜子面前站一天，“它们来了哥哥，总有东西喜欢打断我们欣赏啊，”三线超级魅惑机枪射手离开镜子走向前院，或许，这是唯一能打断他们照镜子的事情了</color>");
        }
    }

    public class HypnoSuperThreeGatling : MonoBehaviour
    {
        public static int PlantID = 1932;

        public void Awake()
        {
            plant.shoot = plant.gameObject.transform.FindChild("headPos2/ThreePeater_head2/ThreePeater_mouth/Shoot");
        }

        public SuperThreeGatling plant => gameObject.GetComponent<SuperThreeGatling>();
    }

    [HarmonyPatch(typeof(ThreePeater), nameof(ThreePeater.GetBulletType))]
    public class ThreePeater_GetBulletType
    {
        [HarmonyPrefix]
        public static bool Prefix(ThreePeater __instance, ref BulletType __result)
        {
            if (__instance != null && (int)__instance.thePlantType == HypnoSuperThreeGatling.PlantID)
            {
                __result = BulletType.Bullet_hypnoPea;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(SuperThreeGatling))]
    public class SuperThreeGatling_SuperShoot
    {
        [HarmonyPatch(nameof(SuperThreeGatling.SuperShoot))]
        [HarmonyPrefix]
        public static bool Prefix(SuperThreeGatling __instance, ref float angle, ref float speed, ref float x, ref float y, ref BulletMoveWay bulletMoveWay, ref int row)
        {
            if (__instance != null && (int)__instance.thePlantType == HypnoSuperThreeGatling.PlantID)
            {
                CreateBullet creator = CreateBullet.Instance;

                Bullet bullet = CreateBullet.Instance.SetBullet(x, y, row, __instance.GetBulletType(), bulletMoveWay, false);
                // 配置子弹属性
                if (bullet != null)
                {
                    // 设置子弹旋转角度
                    bullet.transform.Rotate(0, 0, angle);

                    // 设置子弹移动速度
                    bullet.normalSpeed = speed;

                    // 设置三倍攻击伤害
                    bullet.Damage = 3 * __instance.attackDamage;
                }
                return false;
            }
            return true;
        }

        [HarmonyPatch(nameof(SuperThreeGatling.Update))]
        [HarmonyPrefix]
        public static void Prefix_Update(SuperThreeGatling __instance, out bool __state)
        {
            if (__instance != null && (int)__instance.thePlantType == HypnoSuperThreeGatling.PlantID)
            {
                if (__instance.timer > 0 && __instance.timer - Time.deltaTime <= 0f)
                {
                    __state = true;
                    return;
                }
            }
            __state = false;
        }

        [HarmonyPatch(nameof(SuperThreeGatling.Update))]
        [HarmonyPostfix]
        public static void Postfix_Update(SuperThreeGatling __instance, bool __state)
        {
            if (__state)
                __instance.anim.SetTrigger("shoot");
        }
    }
}