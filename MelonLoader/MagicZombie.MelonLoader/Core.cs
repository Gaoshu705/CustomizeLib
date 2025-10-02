using CustomizeLib.MelonLoader;
using Il2Cpp;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(MagicZombie.MelonLoader.Core), "MagicZombie", "1.0.0", "Salmon", null)]
[assembly: MelonGame("LanPiaoPiao", "PlantsVsZombiesRH")]

namespace MagicZombie.MelonLoader
{
    public class Core : MelonMod
    {
        public override void OnInitializeMelon()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var ab = CustomCore.GetAssetBundle(MelonAssembly.Assembly, "magiczombie");
            CustomCore.RegisterCustomSprite(500, ab.GetAsset<Texture2D>("head").ToSprite());
            CustomCore.RegisterCustomZombie<NormalZombie, MagicZombie>((ZombieType)MagicZombie.ZombieID,
                ab.GetAsset<GameObject>("MagicZombie"), 500, 50, 1350, 0, 0);
            CustomCore.AddZombieAlmanacStrings(MagicZombie.ZombieID, "魔术僵尸", "魔术技巧！喜好对植物表演的大魔术师。\n\n<color=#3D1400>头套贴图作者：@林秋AutumnLin @E杯芒果奶昔 </color>\n<color=#3D1400>韧性：</color><color=red>12000</color>\n<color=#3D1400>特点：</color><color=red>钻石盲盒僵尸生成时有10%伴生，死亡时生成3个钻石套娃僵尸。免疫击退、冰冻、红温，遇到小推车时会将其拾起并回满血，此后啃咬植物直接代码杀，此状态下若再次遇到小推车则将所有小推车变成黑曜石套娃僵尸</color>\n<color=#3D1400>黑曜石套娃僵尸对自己的头套十分满意。这不仅是因为在外观上无可挑剔，更是因为层层嵌套让他无懈可击。</color>");
            CustomCore.AddFusion((int)PlantType.GatlingPea, (int)PlantType.GatlingPea, (int)PlantType.GatlingPea);
            CustomCore.RegisterCustomFusionEvent(PlantType.GatlingPea, PlantType.GatlingPea, (_, _) =>
            {
                MelonLogger.Msg("call");
            });
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (Input.GetKeyDown(KeyCode.K))
            {
                CreateZombie.Instance.SetZombie(Mouse.Instance.theMouseRow, (ZombieType)MagicZombie.ZombieID, Mouse.Instance.mouseX);
            }
        }
    }

    [RegisterTypeInIl2Cpp]
    public class MagicZombie : MonoBehaviour
    {
        public static int ZombieID = 80;

        public void Awake()
        {
            if (GameAPP.theGameStatus == GameStatus.InGame && zombie is not null)
            {
                zombie.butterHead = gameObject.transform.FindChild("Zombie_head/hat").gameObject;
            }
        }

        public NormalZombie? zombie => gameObject.TryGetComponent<NormalZombie>(out var z) ? z : null;
    }
}