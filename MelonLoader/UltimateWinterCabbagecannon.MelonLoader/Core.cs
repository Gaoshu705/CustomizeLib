using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using Microsoft.VisualBasic;
using UnityEngine;
using UnityEngine.UIElements;

[assembly: MelonInfo(typeof(UltimateWinterCabbagecannon.MelonLoader.Core), "UltimateWinterCabbagecannon", "1.0.0", "Salmon", null)]
[assembly: MelonGame("LanPiaoPiao", "PlantsVsZombiesRH")]

namespace UltimateWinterCabbagecannon.MelonLoader
{
    public class Core : MelonMod
    {
        public override void OnInitializeMelon()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var ab = CustomCore.GetAssetBundle(Tools.GetAssembly(), "ultimatewintercabbagecannon");
            CustomCore.RegisterCustomBullet<Bullet_cabbage>(UltimateWinterCabbagecannon.BulletID, ab.GetAsset<GameObject>("Bullet_iceDoomCabbage"));
            CustomCore.RegisterCustomPlant<CabbageCannon, UltimateWinterCabbagecannon>((int)UltimateWinterCabbagecannon.PlantID, ab.GetAsset<GameObject>("UltimateWinterCabbagecannonPrefab"),
                ab.GetAsset<GameObject>("UltimateWinterCabbagecannonPreview"), new List<(int, int)>
                {
                    ((int)PlantType.UltimateCannon, (int)PlantType.Cabbagepult)
                }, 0.75f, 0f, 300, 300, 7.5f, 575);
            CustomCore.RegisterCustomPlantSkin<CabbageCannon, UltimateWinterCabbagecannon>((int)UltimateWinterCabbagecannon.PlantID, ab.GetAsset<GameObject>("UltimateWinterCabbagecannonSkinPrefab"),
                ab.GetAsset<GameObject>("UltimateWinterCabbagecannonSkinPreview"), new List<(int, int)>
                {
                    ((int)PlantType.UltimateCannon, (int)PlantType.Cabbagepult)
                }, 0.75f, 0f, 300, 300, 7.5f, 575);
            CustomCore.AddPlantAlmanacStrings((int)UltimateWinterCabbagecannon.PlantID,
                $"究极冷寂迫击炮",
                "抛投卷心菜造成冷寂杀伤，炮口启动僚机后，会进行覆盖打击。\n" +
                "<color=#0000FF>究级冷寂加农炮同人亚种</color>\n\n" +
                "<color=#3D1400>贴图作者：@屑红leong、@林秋-AutumnLin</color>\n" +
                "<color=#3D1400>转换配方：</color><color=red>玉米投手←→卷心菜投手\n" +
                "*转化为究极冷寂加农炮时，装填时间重置为35秒</color>\n" +
                "<color=#3D1400>伤害：</color><color=red>300/0.75秒</color>\n" +
                "<color=#3D1400>索敌范围：</color><color=red>附近三行前方区域</color>\n" +
                "<color=#3D1400>特性：</color><color=red>巨型</color>\n" +
                "<color=#3D1400>特点：</color><color=red>①攻击时，向索敌范围内每只僵尸各投掷一发冷寂卷心菜\n" +
                "②攻击时有5%概率启动僚机，发射2发攻击力6倍的集束炮弹，随后降落18枚子炮弹，二者命中均施加5秒冻结状态，对于免疫冻结状态的僵尸伤害x8</color>\n" +
                "<color=#3D1400>冷寂卷心菜：</color><color=red>①命中时先施加寒冷状态，赋予25点冻结值，然后造成半径1格爆炸伤害，对冻结单位伤害x4\n" +
                "②本行前方的卷心菜保护伞或绿宝石伞，可以承接本应向其右侧范围投掷的爆破卷心菜并弹射</color>\n" +
                "<color=#3D1400>词条1：</color><color=red>兵贵神速：攻击速度+100%</color>\n" +
                "<color=#3D1400>词条2：</color><color=red>中心爆破：子弹和炮弹的伤害x4</color>\n\n" +
                "<color=#3D1400>咕咕咕</color>"
            );
            CustomCore.AddFusion((int)PlantType.UltimateCannon, (int)UltimateWinterCabbagecannon.PlantID, (int)PlantType.Cornpult);
            CustomCore.RegisterCustomParticle(UltimateWinterCabbagecannon.ParticleID, ab.GetAsset<GameObject>("IceDoomCabbageBomb"));
            ab.GetAsset<GameObject>("IceDoomCabbageBomb").AddComponent<BombCherry>();
            CustomCore.TypeMgrExtra.IsIcePlant.Add(UltimateWinterCabbagecannon.PlantID);
            CustomCore.TypeMgrExtra.DoubleBoxPlants.Add(UltimateWinterCabbagecannon.PlantID);
            CustomCore.AddUltimatePlant(UltimateWinterCabbagecannon.PlantID);
            CustomCore.RegisterCustomOnMixEvent(UltimateWinterCabbagecannon.PlantID, PlantType.Cornpult, (p) =>
            {
                p.GetComponent<UltimateCannon>().firstLoad = false;
            });
            UltimateWinterCabbagecannon.sub = ab.GetAsset<GameObject>("Bullet_subIceDoomCabbage");
            UltimateWinterCabbagecannon.sub.AddComponent<SubCabbage>();
        }
    }

    [RegisterTypeInIl2Cpp]
    public class UltimateWinterCabbagecannon : MonoBehaviour
    {
        public static PlantType PlantID = (PlantType)1956;
        public static BulletType BulletID = (BulletType)1956;
        public static ParticleType ParticleID = (ParticleType)1956;
        public static GameObject? sub = null;

        public CabbageCannon plant => gameObject.GetComponent<CabbageCannon>();
        public float originSpeed = 1f;

        public void Awake()
        {
            plant.shoot = transform.FindChild("Cabbagepult_cabbage _2");
        }

        public void Update()
        {
            if (plant != null && GameAPP.theGameStatus == GameStatus.InGame)
            {
                if (Lawnf.TravelUltimate((UltiBuffs)15))
                    plant.thePlantAttackCountDown -= Time.deltaTime;
            }
        }

        public void AnimSuper_IceDoomCabbagecannon()
        {
            var zombies = Lawnf.GetAllZombies().ToSystemList().Where(z => z != null && !z.isMindControlled).ToList();
            for (int i = 1; i <= 20; i++)
            {
                if (sub == null) continue;
                var cabbage = Instantiate(sub, Vector3.zero, Quaternion.identity, plant.board.transform).GetComponent<SubCabbage>();
                if (cabbage != null)
                {
                    int damage = plant.attackDamage;
                    cabbage.board = plant.board;
                    cabbage.plantType = plant.thePlantType;
                    if (i % 10 == 0)
                        cabbage.iceDoom = true;
                }
            }
        }

        public void SuperStart()
        {
            plant.attributeCountdown = 0.666f;
        }

        public void ShootStart()
        {
            originSpeed = plant.anim.speed;
            if (Lawnf.TravelUltimate((UltiBuffs)15))
            {
                plant.anim.speed = originSpeed * 2;
            }
        }

        public void ShootEnd()
        {
            plant.anim.speed = originSpeed;
        }
    }

    [RegisterTypeInIl2Cpp]
    public class SubCabbage : MonoBehaviour
    {
        public int damage = 300;
        public Board? board = null;
        public Vector3 targetPosition;
        public PlantType plantType = UltimateWinterCabbagecannon.PlantID;
        public bool iceDoom = false;
        public float speed = 10f;
        public int row = 0;

        public void Start()
        {
            if (board == null) Destroy(gameObject);

            if (Lawnf.TravelUltimate(14))
                damage *= 4;
            SetPosition();
        }

        public void SetPosition()
        {
            var list = Lawnf.GetAllZombies().ToSystemList().Where(z => z != null && !z.isMindControlled).ToList();
            Vector3 position;
            if (list.Count > 0)
            {
                var zombie = list[UnityEngine.Random.Range(0, list.Count)];
                position = zombie.axis.transform.position;
                targetPosition = zombie.axis.transform.position;
            }
            else
            {
                targetPosition = new Vector3(UnityEngine.Random.Range(board.boardMinX, board.boardMaxX), UnityEngine.Random.Range(board.boardMinY, board.boardMaxY));
                position = new Vector3(UnityEngine.Random.Range(board.boardMinX, board.boardMaxX) + UnityEngine.Random.Range(-1.1f, 1.1f), board.boardMaxY + UnityEngine.Random.Range(1.8f, 2.7f));
            }
            transform.position = position + new Vector3(0f, UnityEngine.Random.Range(5.2f, 6.7f), 0f) + new Vector3(UnityEngine.Random.Range(-3.0f, 3.0f), UnityEngine.Random.Range(0.0f, 6.0f));
            row = Mouse.Instance.GetRowFromY(position.x, position.y);
        }

        public void Update()
        {
            if (GameAPP.theGameStatus != GameStatus.InGame) return;

            if (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
                Vector2 direction = targetPosition - transform.position;
                if (direction.magnitude > 0.1f)
                {
                    // 计算需要旋转的角度（使右侧指向目标）
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

                    // 应用旋转（只绕Z轴旋转，适合2D游戏）
                    transform.rotation = Quaternion.Euler(0, 0, angle);
                }
            }
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
                Die();
        }

        public void Die()
        {
            if (iceDoom)
            {
                board?.SetDoom(0, 0, false, true, transform.position, 1800, fromType: plantType);
            }
            else
            {
                Action<Zombie> action = (z) =>
                {
                    z.SetFreeze(5f);
                    z.SetCold(10f);
                    z.AddfreezeLevel(25);
                    if (z.freezeTimer > 0f)
                        z.TakeDamage(DmgType.Carred, damage * 4, plantType);
                    else
                        z.TakeDamage(DmgType.Carred, damage * 8, plantType);
                };
                var cherry = board?.CreateCherryExplode(transform.position, row, CherryBombType.IceCharry, fromType: plantType, action: action);
                if (cherry == null) Destroy(gameObject);
                cherry.range = 1.5f;
                cherry.fromType = plantType;
                cherry.explodeDamage = damage;
                cherry.maxRow = 1;
            }
            Destroy(gameObject);
        }
    }

    [HarmonyPatch(typeof(CabbageCannon))]
    public static class CabbageCannonPatch
    {
        [HarmonyPatch(nameof(CabbageCannon.GetBulletType))]
        [HarmonyPrefix]
        public static bool Prefix(CabbageCannon __instance, ref BulletType __result)
        {
            if (__instance != null && __instance.thePlantType == UltimateWinterCabbagecannon.PlantID)
            {
                __result = UltimateWinterCabbagecannon.BulletID;
                return false;
            }
            return true;
        }

        [HarmonyPatch(nameof(CabbageCannon.Shoot1))]
        [HarmonyPostfix]
        public static void PostShoot(CabbageCannon __instance)
        {
            if (__instance != null && __instance.thePlantType == UltimateWinterCabbagecannon.PlantID && UnityEngine.Random.Range(0, 100) < 5 && __instance.attributeCountdown <= 0f)
            {
                __instance.anim.SetTrigger("super");
            }
        }
    }

    [HarmonyPatch(typeof(Bullet_cabbage))]
    public static class Bullet_cabbagePatch
    {
        [HarmonyPatch(nameof(Bullet_cabbage.HitZombie))]
        [HarmonyPrefix]
        public static bool PreZombie(Bullet_cabbage __instance, ref Zombie zombie)
        {
            if (__instance.theBulletType == UltimateWinterCabbagecannon.BulletID)
            {
                if (zombie != null)
                {
                    var cherry = CreateParticle.SetParticle((int)UltimateWinterCabbagecannon.ParticleID, __instance.transform.position, 11).GetComponent<BombCherry>();
                    int damage = __instance.Damage;
                    if (Lawnf.TravelUltimate((UltiBuffs)14))
                        damage *= 4;
                    cherry.explodeDamage = damage;
                    cherry.bombRow = __instance.theBulletRow;
                    cherry.bombType = CherryBombType.IceCharry;
                    cherry.range = 1f;
                    cherry.fromType = __instance.fromType;
                    cherry.maxRow = 1;
                    Action<Zombie> action = (z) =>
                    {
                        z.SetCold(10f);
                        z.AddfreezeLevel(25);
                        if (z.freezeTimer > 0f)
                            z.TakeDamage(DmgType.NormalAll, damage * 4, __instance.fromType);
                        else
                            z.TakeDamage(DmgType.NormalAll, damage, __instance.fromType);
                    };
                    cherry.zombieAction = action;
                    __instance.Die();
                }
                return false;
            }
            return true;
        }

        [HarmonyPatch(nameof(Bullet_cabbage.HitLand))]
        [HarmonyPrefix]
        public static bool PreLand(Bullet_cabbage __instance)
        {
            if (__instance.theBulletType == UltimateWinterCabbagecannon.BulletID)
            {
                var cherry = CreateParticle.SetParticle((int)UltimateWinterCabbagecannon.ParticleID, __instance.transform.position, 11).GetComponent<BombCherry>();
                int damage = __instance.Damage;
                if (Lawnf.TravelUltimate((UltiBuffs)14))
                    damage *= 4;
                cherry.explodeDamage = damage;
                cherry.bombRow = __instance.theBulletRow;
                cherry.bombType = CherryBombType.IceCharry;
                cherry.range = 1f;
                cherry.fromType = __instance.fromType;
                cherry.maxRow = 1;
                Action<Zombie> action = (z) =>
                {
                    z.SetCold(10f);
                    z.AddfreezeLevel(25);
                    if (z.freezeTimer > 0f)
                        z.TakeDamage(DmgType.NormalAll, damage * 4, __instance.fromType);
                    else
                        z.TakeDamage(DmgType.NormalAll, damage, __instance.fromType);
                };
                cherry.zombieAction = action;
                __instance.Die();
                return false;
            }
            return true;
        }
    }
}