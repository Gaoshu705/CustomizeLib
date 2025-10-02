using BepInEx;
using BepInEx.Unity.IL2CPP;
using CustomizeLib.BepInEx;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System.Reflection;
using UnityEngine;

namespace ObsidianThreeGatling.BepInEx
{
    [BepInPlugin("salmon.obsidianthreegatling", "ObsidianThreeGatling", "1.0")]
    public class Core : BasePlugin
    {
        public override void Load()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            ClassInjector.RegisterTypeInIl2Cpp<ObsidianThreeGatling>();
            var ab = CustomCore.GetAssetBundle(Assembly.GetExecutingAssembly(), "obsidianthreegatling");
            CustomCore.RegisterCustomPlant<SuperThreeGatling, ObsidianThreeGatling>(ObsidianThreeGatling.PlantID,
                ab.GetAsset<GameObject>("ObsidianThreeGatlingPrefab"),
                ab.GetAsset<GameObject>("ObsidianThreeGatlingPreview"),
                new List<(int, int)>
                {
                    (1920, 1921),
                    (1921, 1920)
                }, 1.5f, 0f, 120, 8000, 0f, 1950
            );
            CustomCore.AddPlantAlmanacStrings(ObsidianThreeGatling.PlantID, $"究极黑曜石三线机枪射手({ObsidianThreeGatling.PlantID})",
                "向三行发射黑曜石子弹的超级机枪射手。\n\n" +
                "<color=#3D1400>贴图作者：@林秋-AutumnLin</color>\n" +
                "<color=#3D1400>韧性：</color><color=red>8000</color>\n" +
                "<color=#3D1400>伤害：</color><color=red>(120x6)x3/1.5秒</color>\n" +
                "<color=#3D1400>特点：</color><color=red>①无法被承伤，血量高于500时受到致命伤害时，此次伤害不会死亡且血量将至500。\n" +
                "②子弹附带轻微击退，可以穿透2次，每次穿透后伤害减半。\n" +
                "③血量高于500血时每次发射受伤50，每受伤2000韧性时，子弹伤害永久增加40，上限为360点。\n" +
                "④每次攻击有2%概率开大，5秒内，每0.02秒发射伤害为3倍的黑曜石子弹。开大时每次发射受伤30点，每30次发射回复750点血量。\n" +
                "<color=#3D1400>词条1：</color><color=red>狂战士：血量越低防御力越高，血量不高于500时，免疫低于1000的伤害，子弹伤害上限增至1440。2级时，每次攻击开大的概率增加至10%，子弹取消伤害上限</color>\n" +
                "<color=#3D1400>词条2：</color><color=red>阻冲之：子弹伤害和击退距离x3</color>\n" +
                "<color=#3D1400>融合配方：</color><color=red>火焰三线超级机枪射手+寒冰三线超级机枪射手</color>\n\n" +
                "<color=#3D1400>“世界向左，僵尸向右，不论你加入哪一方，请谨记，植物服务于每一个人”“你们不要再祈求和平与健康，我们可以不来叨扰植物，但是我们的身体无法回到健康的时候了，你们也不要来问我我们何时会停止进攻，僵尸的字典没有停止，从清晨踏入前院开始，到夜晚的蹦迪飞跃至屋顶的那一刻，僵尸不再相信善意与理智，我们只会遵从自己的本能，我们别无选择，我们不止要保护戴夫的院子，要征服小区，政府世界，让全球所有的人类看到我们的决心”</color>");
        }
    }

    public class ObsidianThreeGatling : MonoBehaviour
    {
        public static int PlantID = 1930;

        public int totalDamage = 0;
        public int attackTimes = 0;
        public bool init = false;

        public ObsidianThreeGatling() : base(ClassInjector.DerivedConstructorPointer<ObsidianThreeGatling>()) => ClassInjector.DerivedConstructorBody(this);

        public ObsidianThreeGatling(IntPtr i) : base(i)
        {
        }

        public void Awake()
        {
            plant.shoot = plant.gameObject.transform.FindChild("headPos2/ThreePeater_head2/ThreePeater_mouth/Shoot");
        }

        public void Update()
        {
            if (!init)
            {
                totalDamage = plant.attributeCount;
                int damage = totalDamage / 2000 * 40 + 120;
                if (damage < 0 || damage > int.MaxValue)
                    damage = int.MaxValue;
                if (Lawnf.TravelUltimateLevel(21) == 2)
                    plant.attackDamage = damage;
                else if (Lawnf.TravelUltimate(21))
                    plant.attackDamage = damage > 1440 ? 1440 : damage;
                else
                    plant.attackDamage = damage > 360 ? 360 : damage;
                init = true;
            }
        }

        public Bullet AnimShoot_ObsidianThreeGatling()
        {
            if (plant.timer > 0f)
                return null;
            if (UnityEngine.Random.Range(0, 100) < 2 || (Lawnf.TravelUltimateLevel(21) == 2 && UnityEngine.Random.Range(0, 100) < 10))
            {
                plant.timer = 5f;
                plant.flashCountDown = 5f;
                plant.AttributeEvent();
                plant.anim?.SetBoolString("shooting", true);
                plant.Recover(plant.thePlantMaxHealth);
                return null;
            }
            else
            {
                Vector3 shoot = plant.shoot.transform.position;
                Bullet bullet = ShootThis(shoot.x + 0.1f, shoot.y, plant.thePlantRow);

                GameAPP.PlaySound(UnityEngine.Random.Range(3, 5), 0.5f, 1f);

                if (plant.thePlantRow == 0)
                {
                    ShootLower(shoot.x + 0.1f, shoot.y, plant.thePlantRow + 1);
                    ShootThis(shoot.x + 0.1f, shoot.y, plant.thePlantRow);
                }
                else if (plant.thePlantRow == plant.board.rowNum - 1)
                {
                    ShootUpper(shoot.x + 0.1f, shoot.y, plant.thePlantRow - 1);
                    ShootThis(shoot.x + 0.1f, shoot.y, plant.thePlantRow);
                }
                else
                {
                    ShootLower(shoot.x + 0.1f, shoot.y, plant.thePlantRow + 1);
                    ShootUpper(shoot.x + 0.1f, shoot.y, plant.thePlantRow - 1);
                }

                return bullet;
            }
        }

        public void ShootLower(float X, float Y, int row)
        {
            if (plant.thePlantHealth > 500)
                plant.TakeDamage(50);
            Bullet bullet = CreateBullet.Instance.SetBullet(X, Y, row, plant.GetBulletType(), BulletMoveWay.Three_down);
            if (bullet is not null)
                bullet.Damage = plant.attackDamage;
        }

        public void ShootUpper(float X, float Y, int row)
        {
            if (plant.thePlantHealth > 500)
                plant.TakeDamage(50);
            Bullet bullet = CreateBullet.Instance.SetBullet(X, Y, row, plant.GetBulletType(), BulletMoveWay.Three_up);
            if (bullet is not null)
                bullet.Damage = plant.attackDamage;
        }

        public Bullet ShootThis(float X, float Y, int row)
        {
            if (plant.thePlantHealth > 500)
                plant.TakeDamage(50);
            Bullet bullet = CreateBullet.Instance.SetBullet(X, Y, row, plant.GetBulletType(), BulletMoveWay.MoveRight);
            if (bullet is not null)
                bullet.Damage = plant.attackDamage;
            return bullet;
        }

        public SuperThreeGatling plant => gameObject.GetComponent<SuperThreeGatling>();
    }

    [HarmonyPatch(typeof(ThreePeater), nameof(ThreePeater.GetBulletType))]
    public class ThreePeater_GetBulletType
    {
        [HarmonyPrefix]
        public static bool Prefix(ThreePeater __instance, ref BulletType __result)
        {
            if (__instance != null && (int)__instance.thePlantType == ObsidianThreeGatling.PlantID)
            {
                __result = BulletType.Bullet_steelPea;
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
            if (__instance != null && (int)__instance.thePlantType == ObsidianThreeGatling.PlantID)
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
                if (__instance.thePlantHealth > 500)
                {
                    __instance.thePlantHealth -= 10;
                    if (__instance.TryGetComponent<ObsidianThreeGatling>(out var comp) && comp != null &&
                        comp.totalDamage + 10 > 0 && comp.totalDamage + 10 <= int.MaxValue &&
                        __instance.attributeCount + 10 > 0 && __instance.attributeCount + 10 <= int.MaxValue)
                    {
                        comp.totalDamage += 10;
                        __instance.attributeCount = comp.totalDamage;
                    }
                    __instance.UpdateText();
                }
                ObsidianThreeGatling component = __instance.GetComponent<ObsidianThreeGatling>();
                if (component != null)
                {
                    component.attackTimes++;
                    if (component.attackTimes >= 30)
                    {
                        __instance.Recover(750f);
                        component.attackTimes = 0;
                    }
                }
                return false;
            }
            return true;
        }

        [HarmonyPatch(nameof(SuperThreeGatling.Update))]
        [HarmonyPrefix]
        public static void Prefix_Update(SuperThreeGatling __instance, out bool __state)
        {
            if (__instance != null && (int)__instance.thePlantType == ObsidianThreeGatling.PlantID)
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

        [HarmonyPatch(nameof(SuperThreeGatling.TakeDamage))]
        [HarmonyPrefix]
        public static bool Prefix(SuperThreeGatling __instance, ref int damage)
        {
            if (__instance != null && (int)__instance.thePlantType == ObsidianThreeGatling.PlantID)
            {
                if (__instance.thePlantHealth > 500 && __instance.thePlantHealth - damage <= 0)
                {
                    if (__instance.TryGetComponent<ObsidianThreeGatling>(out var comp) && comp != null)
                    {
                        comp.totalDamage += damage - __instance.thePlantHealth;
                        __instance.attributeCount = comp.totalDamage;
                    }
                    damage = 0;
                    __instance.thePlantHealth = 500;
                    __instance.UpdateText();
                    return false;
                }

                if (Lawnf.TravelUltimate(21))
                {
                    if (__instance.thePlantHealth <= 500 && damage <= 500)
                    {
                        damage = 0;
                        return false;
                    }
                    float damageTimes = 1f - (__instance.thePlantMaxHealth - __instance.thePlantHealth) / (__instance.thePlantMaxHealth * 0.01f) * 0.01f;
                    damage = (int)(damage * damageTimes);
                }

                if (__instance.TryGetComponent<ObsidianThreeGatling>(out var component) && component is not null)
                {
                    if (component.totalDamage + damage > 0 && component.totalDamage + damage <= int.MaxValue &&
                        __instance.attributeCount + damage > 0 && __instance.attributeCount + damage <= int.MaxValue)
                        component.totalDamage += damage;
                    __instance.attributeCount = component.totalDamage;
                    int dmg = component.totalDamage / 2000 * 40 + 120;
                    if (Lawnf.TravelUltimateLevel(21) == 2)
                        __instance.attackDamage = dmg;
                    else if (Lawnf.TravelUltimate(21))
                        __instance.attackDamage = dmg > 1440 ? 1440 : dmg;
                    else
                        __instance.attackDamage = dmg > 360 ? 360 : dmg;
                }
            }
            return true;
        }

        [HarmonyPatch(nameof(SuperThreeGatling.Crashed))]
        [HarmonyPrefix]
        public static bool Prefix(SuperThreeGatling __instance)
        {
            if (__instance != null && (int)__instance.thePlantType == ObsidianThreeGatling.PlantID)
            {
                if (__instance.thePlantHealth > 500)
                {
                    if (__instance.TryGetComponent<ObsidianThreeGatling>(out var component) && component != null)
                    {
                        component.totalDamage += __instance.thePlantHealth - 500;
                        __instance.attributeCount = component.totalDamage;
                    }
                    __instance.thePlantHealth = 500;
                    __instance.UpdateText();
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.Instead))]
    public class Plant_Instead
    {
        [HarmonyPrefix]
        public static bool Prefix(Plant __instance, ref bool __result)
        {
            if (__instance != null && (int)__instance.thePlantType == ObsidianThreeGatling.PlantID)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
