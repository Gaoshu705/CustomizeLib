using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using System.Linq.Expressions;
using UnityEngine;

[assembly: MelonInfo(typeof(PuffUltimateGatling.Core), "PuffUltimateGatling", "1.0.0", "Administrator", null)]
[assembly: MelonGame("LanPiaoPiao", "PlantsVsZombiesRH")]

namespace PuffUltimateGatling
{
    public class Core : MelonMod
    {
        public override void OnInitializeMelon()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var ab = CustomCore.GetAssetBundle(MelonAssembly.Assembly, "puffultimategatling");
            CustomCore.RegisterCustomBullet<Bullet_superCherry, Bullet_puffSuperCherry>((BulletType)Bullet_puffSuperCherry.BulletID, ab.GetAsset<GameObject>("Bullet_pufSuperfCherry"));
            CustomCore.RegisterCustomPlant<UltimateGatling, PuffUltimateGatling>(
                PuffUltimateGatling.PlantID,
                ab.GetAsset<GameObject>("PuffUltimateGatlingPrefab"),
                ab.GetAsset<GameObject>("PuffUltimateGatlingPreview"),
                new List<(int, int)>
                {
                    ((int)PlantType.SmallPuff, (int)PlantType.UltimateGatling)
                },
                1.5f, 0f, 200, 300, 0f, 950
            );
            CustomCore.RegisterCustomPlantSkin<UltimateGatling, PuffUltimateGatling>(
                PuffUltimateGatling.PlantID,
                ab.GetAsset<GameObject>("PuffUltimateGatlingPrefabSkin"),
                ab.GetAsset<GameObject>("PuffUltimateGatlingPreviewSkin"),
                new List<(int, int)>
                {
                    ((int)PlantType.SmallPuff, (int)PlantType.UltimateGatling)
                },
                1.5f, 0f, 200, 300, 0f, 950
            );
            CustomCore.AddPlantAlmanacStrings(PuffUltimateGatling.PlantID,
                $"究极小樱桃射手({PuffUltimateGatling.PlantID})",
                "向三行发射小冰锥的小超级机枪射手。\n\n<color=#3D1400>贴图作者：@林秋-AutumnLin</color>\n<color=#3D1400>伤害：</color><color=red>(20x3)x6/1.5秒</color>\n<color=#3D1400>特点：</color><color=red>每次攻击有2%概率触发大招，5秒内，每0.02秒向三行各发射1个伤害为3倍的小冰锥。</color>\n<color=#3D1400>融合配方：</color><color=red>小喷菇（底座）+三线超级寒冰机枪射手</color>\n\n<color=#3D1400>咕咕嘎嘎</color>"
            );
            CustomCore.TypeMgrExtra.IsPuff.Add((PlantType)PuffUltimateGatling.PlantID);
        }
    }

    [RegisterTypeInIl2Cpp]
    public class PuffUltimateGatling : MonoBehaviour
    {
        public static int PlantID = 1929;

        public void Awake()
        {
            plant.shoot = plant.gameObject.transform.GetChild(0);
            plant.isShort = true;
        }
        public UltimateGatling plant => gameObject.GetComponent<UltimateGatling>();
    }

    [RegisterTypeInIl2Cpp]
    public class Bullet_puffSuperCherry : MonoBehaviour
    {
        public static int BulletID = 1929;

        public Bullet_superCherry bullet => gameObject.GetComponent<Bullet_superCherry>();
    }

    [HarmonyPatch(typeof(SuperCherryShooter), nameof(SuperCherryShooter.Shoot1))]
    public class SuperCherryShooter_Shoot1_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(SuperCherryShooter __instance, ref Bullet __result)
        {
            if ((int)__instance.thePlantType == PuffUltimateGatling.PlantID)
            {
                // 创建樱桃子弹
                Bullet bullet = CreateBullet.Instance.SetBullet(__instance.shoot.position.x, __instance.shoot.position.y, __instance.thePlantRow, (BulletType)Bullet_puffSuperCherry.BulletID, BulletMoveWay.MoveRight);

                if (bullet == null)
                {
                    __result = null;
                    return false;
                }

                // 设置子弹伤害
                bullet.Damage = __instance.attackDamage;

                // 播放随机射击音效
                GameAPP.PlaySound(UnityEngine.Random.Range(3, 5), 0.5f, 1f);

                __result = bullet;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Bullet_superCherry), nameof(Bullet_superCherry.HitZombie))]
    public class Bullet_superCherry_HitZombie_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(Bullet_superCherry __instance, ref Zombie zombie)
        {
            if ((int)__instance.theBulletType == Bullet_puffSuperCherry.BulletID)
            {
                if (zombie == null)
                    return false;

                // 获取子弹位置并创建爆炸粒子
                Vector3 bulletPos = __instance.transform.position;
                GameObject explosionObj = CreateParticle.SetParticle(14, bulletPos, __instance.theBulletRow, true);

                if (explosionObj == null)
                    return false;

                // 获取炸弹樱桃组件
                BombCherry bombCherry = explosionObj.GetComponent<BombCherry>();
                if (bombCherry == null)
                    return false;

                // 设置爆炸参数
                bombCherry.bombRow = zombie.theZombieRow;  // 爆炸影响的行
                bombCherry.bombType = CherryBombType.Bullet;          // 爆炸类型2：樱桃炸弹

                // 基础伤害设置
                bombCherry.explodeDamage = __instance.Damage;

                // 根据特殊状态调整爆炸参数
                switch (__instance.rogueStatus)
                {
                    case 1: // 状态1：双倍伤害，1.5倍范围，类型2
                        bombCherry.explodeDamage = __instance.Damage * 2;
                        bombCherry.range *= 1.5f;
                        bombCherry.bombType = CherryBombType.Bullet;
                        break;

                    case 2: // 状态2：四倍伤害，0.5倍范围，类型1
                        bombCherry.explodeDamage = __instance.Damage * 4;
                        bombCherry.range *= 0.5f;
                        bombCherry.bombType = CherryBombType.Sun;
                        break;

                    case 3: // 状态3：基础伤害，其他参数不变
                        bombCherry.explodeDamage = __instance.Damage;
                        break;
                }

                // 特殊僵尸状态处理
                if (zombie.theStatus == ZombieStatus.Flying) // 状态10的僵尸
                {
                    bombCherry.bombType = CherryBombType.BulletAll; // 使用特殊爆炸类型5
                }

                // 僵尸子弹标记传递
                if (__instance.isZombieBullet)
                {
                    bombCherry.isFromZombie = true; // 标记为僵尸方炸弹
                }

                // 播放爆炸音效
                GameAPP.PlaySound(40, 0.2f, 1f);

                // 销毁子弹
                __instance.Die();
                return false;
            }
            return true;
        }
    }
}