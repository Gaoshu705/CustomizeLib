// #define DEBUG_FEATURE__ENABLE_MULTI_LEVEL_BUFF // 启用多级词条（旧版实现）

using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppTMPro;
using MelonLoader;
using MelonLoader.Utils;
using SevenZip.Compression.LZ;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static Il2CppSystem.Runtime.Remoting.RemotingServices;
using static MelonLoader.MelonLogger;
using static UnityEngine.Object;

///
/// Specially credit to 暗影Dev
/// Specially credit to likefengzi(https://github.com/likefengzi)(https://space.bilibili.com/237491236)
/// Specially credit to BloomsTeam
///
namespace CustomizeLib.MelonLoader
{
    /// <summary>
    /// 注册融合洋芋配方
    /// </summary>
    [HarmonyPatch(typeof(MixBomb), nameof(MixBomb.AttributeEvent))]
    public class MixBombPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(MixBomb __instance)
        {
            bool success = false;
            if (__instance != null)
            {
                List<Plant> plants = Lawnf.Get1x1Plants(__instance.thePlantColumn, __instance.thePlantRow).ToArray().ToList();
                if (plants is null)
                    return true;
                foreach (Plant plant in plants)
                {
                    if (plant != null && CustomCore.CustomMixBombFusions.Keys.Any(k => k.Item2 == plant.thePlantType))
                    {
                        List<(PlantType, PlantType, PlantType)> mixBombFusions = CustomCore.CustomMixBombFusions
                            .Where(kvp => kvp.Key.Item2 == plant.thePlantType)
                            .Select(kvp => kvp.Key)
                            .ToList();
                        List<Plant> leftPlant = Lawnf.Get1x1Plants(__instance.thePlantColumn - 1, __instance.thePlantRow).ToArray().ToList();
                        List<Plant> rightPlant = Lawnf.Get1x1Plants(__instance.thePlantColumn + 1, __instance.thePlantRow).ToArray().ToList();
                        foreach ((PlantType, PlantType, PlantType) fusion in mixBombFusions)
                        {
                            Plant? firstLeftPlant = leftPlant.FirstOrDefault(p => p.thePlantType == fusion.Item1);
                            Plant? firstRightPlant = rightPlant.FirstOrDefault(p => p.thePlantType == fusion.Item3);
                            if (firstLeftPlant == null || firstRightPlant == null)
                            {
                                CustomCore.CustomMixBombFusions[fusion].Item2[UnityEngine.Random.Range(0, CustomCore.CustomMixBombFusions[fusion].Item2.Count)](firstLeftPlant, plant, firstRightPlant);
                                continue;
                            }
                            if (leftPlant.Any(p => p.thePlantType == fusion.Item1) && rightPlant.Any(p => p.thePlantType == fusion.Item3))
                            {
                                CustomCore.CustomMixBombFusions[fusion].Item1[UnityEngine.Random.Range(0, CustomCore.CustomMixBombFusions[fusion].Item1.Count)](firstLeftPlant, plant, firstRightPlant);
                                success = true;
                            }
                            else
                            {
                                CustomCore.CustomMixBombFusions[fusion].Item2[UnityEngine.Random.Range(0, CustomCore.CustomMixBombFusions[fusion].Item2.Count)](firstLeftPlant, plant, firstRightPlant);
                            }
                        }
                    }
                }
            }
            if (__instance != null && success)
                __instance.Die();
            if (success)
                return false;
            return true;
        }
    }

    /// <summary>
    /// 注册肥料使用事件
    /// </summary>
    [HarmonyPatch(typeof(Fertilize))]
    public class FertilizePatch
    {
        [HarmonyPatch(nameof(Fertilize.Upgrade))]
        [HarmonyPostfix]
        public static void PostUpgrade(Fertilize __instance)
        {
            if (__instance == null || __instance.theTargetPlant == null) return;

            int column = __instance.theTargetPlant.thePlantColumn;
            int row = __instance.theTargetPlant.thePlantRow;

            List<Plant> plants = Lawnf.Get1x1Plants(column, row).ToArray().ToList<Plant>(); // 获取植物，il2cpp窝爱你
            if (plants == null) return;

            for (int i = 0; i < plants.Count; i++)
            {
                Plant plant = plants[i];
                if (plant == null) continue;
                if (plant.thePlantColumn != column || plant.thePlantRow != row) continue;
                if (Board.Instance == null) return;

                if (CustomCore.CustomUseFertilize.ContainsKey(plant.thePlantType))
                {
                    CustomCore.CustomUseFertilize[plant.thePlantType](plant);
                }
            }

            Destroy(__instance.gameObject);
        }
    }

    /// <summary>
    /// 自动拓展植物图鉴大小
    /// </summary>
    [HarmonyPatch(typeof(AlmanacMenu))]
    public static class AlmanacMenuPatch
    {
        [HarmonyPatch(nameof(AlmanacMenu.Awake))]
        [HarmonyPostfix]
        public static void PostAwake(AlmanacMenu __instance)
        {
            __instance.transform.FindChild("AlmanacPlant2").FindChild("Cards").GetComponent<GridManager>().maxY = GameAPP.resourcesManager.allPlants.Count / 9 * 1.5f;
        }
    }

    /// <summary>
    /// 植物图鉴
    /// </summary>
    [HarmonyPatch(typeof(AlmanacPlantBank))]
    public static class AlmanacMgrPatch
    {
        /*
        [HarmonyPatch(nameof(AlmanacPlantBank.RightSkin))]
        [HarmonyPostfix]
        public static void PostRightSkin(AlmanacPlantBank __instance)
        {
            PlantType plantType = (PlantType)__instance.theSeedType;
            if (!GameAPP.resourcesManager.plantSkinDic.TryGetValue(plantType, out var index)) return;
            if (!CustomCore.CustomBulletsSkin.TryGetValue(plantType, out var list)) return;
            if (index >= list.Count) return;
            GameAPP.resourcesManager.bulletPrefabs[list[index].ID] = list[index].Prefab;
            Action<Bullet> action = (bullet) =>
            {
                BulletPoolManager.Instance.CreateBullet(bullet.theBulletType).gameObject.SetActive(true);
            };
            BulletPoolManager.Instance.pools[list[index].ID].m_ActionOnRelease = action;
            Msg($"prefabname = {GameAPP.resourcesManager.bulletPrefabs[list[index].ID].name}, index = {index}");
        }

        [HarmonyPatch(nameof(AlmanacPlantBank.LeftSkin))]
        [HarmonyPostfix]
        public static void PostLeftSkin(AlmanacPlantBank __instance)
        {
            PlantType plantType = (PlantType)__instance.theSeedType;
            if (!GameAPP.resourcesManager.plantSkinDic.TryGetValue(plantType, out var index)) return;
            if (!CustomCore.CustomBulletsSkin.TryGetValue(plantType, out var list)) return;
            if (index >= list.Count) return;
            GameAPP.resourcesManager.bulletPrefabs[list[index].ID] = list[index].Prefab;
            Action<Bullet> action = (bullet) =>
            {
                BulletPoolManager.Instance.CreateBullet(bullet.theBulletType).gameObject.SetActive(true);
            };
            BulletPoolManager.Instance.pools[list[index].ID].m_ActionOnGet = action;
            Msg($"prefabname = {GameAPP.resourcesManager.bulletPrefabs[list[index].ID].name}, index = {index}");
        }
        */
        /// <summary>
        /// 初始化结束显示换肤按钮，加载皮肤
        /// </summary>
        /// <param name="__instance"></param>
        /// <returns></returns>
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void PostStart(AlmanacPlantBank __instance)
        {
            {
                PlantType plantType = (PlantType)__instance.theSeedType;
                if (CustomCore.CustomPlantsSkinActive.ContainsKey(plantType) && !CustomCore.CustomPlantsSkinActive[plantType]);
                {
                    if (CustomCore.CustomPlantsSkin.ContainsKey(plantType))
                        __instance.skinButton.SetActive(CustomCore.CustomPlantsSkin.ContainsKey(plantType));

                    if (CustomCore.CustomPlantsSkin.TryGetValue(plantType, out var data))
                    {
                        if (!GameAPP.resourcesManager.plantSkinDic.TryGetValue((PlantType)__instance.theSeedType, out var _))
                            GameAPP.resourcesManager.plantSkinDic.Add(plantType, 0);
                        foreach (var item in data)
                        {
                            var prefab = item.Prefab;
                            var preview = item.Preview;

                            if (prefab != null)
                            {
                                if (GameAPP.resourcesManager._plantPrefabs.ContainsKey(plantType))
                                    GameAPP.resourcesManager._plantPrefabs[(PlantType)__instance.theSeedType].Add(prefab);
                                else
                                {
                                    Il2CppSystem.Collections.Generic.List<GameObject> list = new();
                                    list.Add(GameAPP.resourcesManager.plantPrefabs[plantType]);
                                    list.Add(prefab);
                                    GameAPP.resourcesManager._plantPrefabs.Add(plantType, list);
                                }
                            }
                            if (preview != null)
                            {
                                if (GameAPP.resourcesManager._plantPreviews.ContainsKey(plantType))
                                    GameAPP.resourcesManager._plantPreviews[(PlantType)__instance.theSeedType].Add(preview);
                                else
                                {
                                    Il2CppSystem.Collections.Generic.List<GameObject> list = new();
                                    list.Add(GameAPP.resourcesManager.plantPreviews[plantType]);
                                    list.Add(preview);
                                    GameAPP.resourcesManager._plantPreviews.Add(plantType, list);
                                }
                            }
                            CustomCore.CustomPlantsSkinActive[plantType] = true;
                        }
                    }
                }
            }
            {
                PlantType plantType = (PlantType)__instance.theSeedType;
                if (CustomCore.CustomPlantsSkinActive.ContainsKey(plantType) && CustomCore.CustomPlantsSkinActive[plantType]) goto DIR_SEARCH;

                if (CustomCore.CustomPlantTypes.Contains(plantType))
                    __instance.skinButton.SetActive(CustomCore.CustomPlantsSkin.ContainsKey(plantType));

                if (!CustomCore.CustomPlantsSkin.TryGetValue(plantType, out var data)) goto DIR_SEARCH;
                if (!GameAPP.resourcesManager.plantSkinDic.TryGetValue((PlantType)__instance.theSeedType, out var _))
                    GameAPP.resourcesManager.plantSkinDic.Add(plantType, 0);

                foreach (var item in data)
                {
                    var prefab = item.Prefab;
                    var preview = item.Preview;

                    if (prefab != null)
                    {
                        if (GameAPP.resourcesManager._plantPrefabs.ContainsKey(plantType))
                            GameAPP.resourcesManager._plantPrefabs[(PlantType)__instance.theSeedType].Add(prefab);
                        else
                        {
                            Il2CppSystem.Collections.Generic.List<GameObject> list = new();
                            list.Add(GameAPP.resourcesManager.plantPrefabs[plantType]);
                            list.Add(prefab);
                            GameAPP.resourcesManager._plantPrefabs.Add(plantType, list);
                        }
                    }
                    if (preview != null)
                    {
                        if (GameAPP.resourcesManager._plantPreviews.ContainsKey(plantType))
                            GameAPP.resourcesManager._plantPreviews[(PlantType)__instance.theSeedType].Add(preview);
                        else
                        {
                            Il2CppSystem.Collections.Generic.List<GameObject> list = new();
                            list.Add(GameAPP.resourcesManager.plantPreviews[plantType]);
                            list.Add(preview);
                            GameAPP.resourcesManager._plantPreviews.Add(plantType, list);
                        }
                    }
                    CustomCore.CustomPlantsSkinActive[plantType] = true;
                }
            }
        DIR_SEARCH:
            {
                PlantType plantType = (PlantType)__instance.theSeedType;
                if (CustomCore.CustomPlantsSkinActive.ContainsKey(plantType) && CustomCore.CustomPlantsSkinActive[plantType]) return;
                String fullName = Directory.GetParent(Application.dataPath)?.FullName;
                if (fullName == null)
                    return;
                string skinPath = Path.Combine(fullName, MelonEnvironment.ModsDirectory, "Skin");
                if (!Directory.Exists(skinPath))
                    return;
                var regex = new Regex($@"^skin_{__instance.theSeedType}(?!\d).*$", RegexOptions.IgnoreCase);
                String[] files = Directory.GetFiles(skinPath).Where(str => regex.IsMatch(Path.GetFileNameWithoutExtension(str))).ToArray();
                foreach (var item in files)
                {
                    AssetBundle ab = AssetBundle.LoadFromFile(item);
                    GameObject? prefab = null;
                    try
                    {
                        prefab = ab.GetAsset<GameObject>("Prefab");
                        prefab.tag = "Plant";
                    }
                    catch
                    {
                        return;
                    }
                    GameObject? preview = null;
                    try
                    {
                        preview = ab.GetAsset<GameObject>("Preview");
                        preview.tag = "Preview";
                    }
                    catch
                    {
                        return;
                    }

                    CustomPlantData newCustomPlantData = new()
                    {
                        ID = (int)plantType,
                        PlantData = PlantDataLoader.plantDatas[plantType],
                        Prefab = GameAPP.resourcesManager.plantPrefabs[plantType],
                        Preview = GameAPP.resourcesManager.plantPreviews[plantType]
                    };

                    if (!GameAPP.resourcesManager.plantSkinDic.TryGetValue((PlantType)__instance.theSeedType, out var _))
                        GameAPP.resourcesManager.plantSkinDic.Add(plantType, 0);

                    if (prefab != null)
                    {
                        GameObject oldPrefab = GameAPP.resourcesManager.plantPrefabs[plantType];
                        var components = oldPrefab.GetComponents<Component>();
                        // 复制旧预制体上的组件
                        foreach (var component in components)
                        {
                            if (!prefab.TryGetComponent(component.GetIl2CppType(), out var comp) && comp == null)
                                prefab.AddComponent(component.GetIl2CppType());
                        }
                        // 赋值植物类型
                        prefab.GetComponent<Plant>().thePlantType = oldPrefab.GetComponent<Plant>().thePlantType;

                        if (GameAPP.resourcesManager._plantPrefabs.ContainsKey(plantType))
                            GameAPP.resourcesManager._plantPrefabs[plantType].Add(prefab);
                        else
                        {
                            Il2CppSystem.Collections.Generic.List<GameObject> list = new();
                            list.Add(GameAPP.resourcesManager.plantPrefabs[plantType]);
                            list.Add(prefab);
                            GameAPP.resourcesManager._plantPrefabs.Add(plantType, list);
                        }
                        prefab.GetComponent<Plant>().FindShoot(prefab.GetComponent<Plant>().transform);
                        newCustomPlantData.Prefab = prefab;
                    }
                    if (preview != null)
                    {
                        if (GameAPP.resourcesManager._plantPreviews.ContainsKey(plantType))
                            GameAPP.resourcesManager._plantPreviews[plantType].Add(preview);
                        else
                        {
                            Il2CppSystem.Collections.Generic.List<GameObject> list = new();
                            list.Add(GameAPP.resourcesManager.plantPreviews[plantType]);
                            list.Add(preview);
                            GameAPP.resourcesManager._plantPreviews.Add(plantType, list);
                        }

                        GameObject oldPreview = GameAPP.resourcesManager.plantPreviews[plantType];
                        var components = oldPreview.GetComponents<Component>();
                        // 复制旧预制体上的组件
                        foreach (var component in components)
                        {
                            if (!preview.TryGetComponent(component.GetIl2CppType(), out var comp) && comp == null)
                                preview.AddComponent(component.GetIl2CppType());
                        }
                        newCustomPlantData.Preview = preview;
                    }
                    __instance.skinButton.SetActive(true);
                    if (CustomCore.CustomPlantsSkin.ContainsKey(plantType))
                        CustomCore.CustomPlantsSkin[plantType].Add(newCustomPlantData);
                    else
                        CustomCore.CustomPlantsSkin.Add(plantType, new List<CustomPlantData> { newCustomPlantData });
                    /*Msg("bullet");
                    GameObject? bulletPrefab = null;
                    BulletType bulletType = (BulletType)(-1);
                    try
                    {
                        var strArray = ab.GetAssetsNames();
                        Regex regex = new(@"^BulletPrefab_(\d+)$");
                        foreach (var str in strArray)
                        {
                            Match match = regex.Match(str);
                            if (match.Success)
                                if (ab.GetAsset<GameObject>(str) != null && int.TryParse(match.Groups[1].Value, out var type))
                                {
                                    bulletPrefab = ab.GetAsset<GameObject>(str);
                                    bulletType = (BulletType)type;
                                }
                        }
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Msg(e);
                    }
                    if (bulletPrefab != null)
                    {
                        foreach (var component in GameAPP.resourcesManager.bulletPrefabs[bulletType].GetComponents<Component>())
                            if (!bulletPrefab.TryGetComponent(component.GetIl2CppType(), out var comp) && comp == null)
                                bulletPrefab.AddComponent(component.GetIl2CppType());
                        CustomCore.RegisterCustomBulletSkin(bulletPrefab, plantType, bulletType);
                    }*/
                }
            }
        }

        /// <summary>
        /// 从json加载植物信息
        /// </summary>
        /// <param name="__instance"></param>
        /// <returns></returns>
        [HarmonyPatch("InitNameAndInfoFromJson")]
        [HarmonyPrefix]
        public static bool PreInitNameAndInfoFromJson(AlmanacPlantBank __instance)
        {
            //如果自定义植物图鉴信息包含
            if (CustomCore.PlantsAlmanac.ContainsKey((PlantType)__instance.theSeedType))
            {
                //遍历图鉴上的组件
                for (int i = 0; i < __instance.transform.childCount; i++)
                {
                    Transform childTransform = __instance.transform.GetChild(i);
                    if (childTransform == null)
                    {
                        continue;
                    }

                    //植物姓名
                    if (childTransform.name == "Name")
                    {
                        childTransform.GetComponent<TextMeshPro>().text =
                            CustomCore.PlantsAlmanac[(PlantType)__instance.theSeedType].Item1;
                        childTransform.GetChild(0).GetComponent<TextMeshPro>().text =
                            CustomCore.PlantsAlmanac[(PlantType)__instance.theSeedType].Item1;
                    }

                    //植物信息
                    if (childTransform.name == "Info")
                    {
                        TextMeshPro info = childTransform.GetComponent<TextMeshPro>();
                        info.overflowMode = TextOverflowModes.Page;
                        info.fontSize = 40;
                        info.text = CustomCore.PlantsAlmanac[(PlantType)__instance.theSeedType].Item2;
                        __instance.introduce = info;
                    }

                    //植物阳光
                    if (childTransform.name == "Cost")
                    {
                        childTransform.GetComponent<TextMeshPro>().text = "";
                    }
                }

                //阻断原始的加载
                return false;
            }
            //如果是二创库里注册的换皮肤植物
            if (CustomCore.CustomPlantsSkinActive.ContainsKey((PlantType)__instance.theSeedType) && CustomCore.PlantsSkinAlmanac.ContainsKey((PlantType)__instance.theSeedType) && CustomCore.CustomPlantsSkinActive[(PlantType)__instance.theSeedType])
            {
                var alm = CustomCore.PlantsSkinAlmanac[(PlantType)__instance.theSeedType];
                if (alm is null) return true;//若无图鉴数据，直接进入函数本体，继续执行原本读取图鉴数据的逻辑
                var almanac = alm.Value;
                //遍历图鉴上的组件
                for (int i = 0; i < __instance.transform.childCount; i++)
                {
                    Transform childTransform = __instance.transform.GetChild(i);
                    if (childTransform == null)
                    {
                        continue;
                    }

                    //植物姓名
                    if (childTransform.name == "Name")
                    {
                        childTransform.GetComponent<TextMeshPro>().text = almanac.Item1;
                        childTransform.GetChild(0).GetComponent<TextMeshPro>().text = almanac.Item1;
                    }

                    //植物信息
                    if (childTransform.name == "Info")
                    {
                        TextMeshPro info = childTransform.GetComponent<TextMeshPro>();
                        info.overflowMode = TextOverflowModes.Page;
                        info.fontSize = 40;
                        info.text = almanac.Item2;
                        __instance.introduce = info;
                    }

                    //植物阳光
                    if (childTransform.name == "Cost")
                    {
                        childTransform.GetComponent<TextMeshPro>().text = "";
                    }
                }

                //阻断原始的加载
                return false;
            }

            return true;
        }

        /// <summary>
        /// 图鉴中鼠标按下，用于翻页
        /// </summary>
        /// <param name="__instance"></param>
        /// <returns></returns>
        [HarmonyPatch("OnMouseDown")]
        [HarmonyPrefix]
        public static bool PreOnMouseDown(AlmanacPlantBank __instance)
        {
            //右侧显示
            __instance.introduce =
                __instance.gameObject.transform.FindChild("Info").gameObject.GetComponent<TextMeshPro>();
            //页数
            __instance.pageCount = __instance.introduce.m_pageNumber * 1;
            //下一页
            if (__instance.currentPage <= __instance.introduce.m_pageNumber)
            {
                ++__instance.currentPage;
            }
            else
            {
                __instance.currentPage = 1;
            }

            //翻页
            __instance.introduce.pageToDisplay = __instance.currentPage;

            //阻断原始翻页
            return false;
        }
    }

    [HarmonyPatch(typeof(Application))]
    public static class ApplicationPatch
    {
        [HarmonyPatch(nameof(Application.Quit), new Type[] { })]
        [HarmonyPrefix]
        public static void PreQuitBase()
        {
            Dictionary<PlantType, int> skinDic = new();
            foreach (var (key, value) in GameAPP.resourcesManager.plantSkinDic)
            {
                if (CustomCore.CustomPlantsSkin.ContainsKey(key))
                {
                    skinDic.Add(key, value);
                }
            }

            var jsonText = JsonSerializer.Serialize(skinDic);
            var directory = Path.Combine(Application.persistentDataPath, "Skin");
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "skin.json");
            if (!File.Exists(path))
                File.Create(path).Dispose();
            File.WriteAllText(path, jsonText);
        }

        [HarmonyPatch(nameof(Application.Quit), new Type[] { typeof(int) })]
        [HarmonyPrefix]
        public static void PreQuitOverride()
        {
            Dictionary<PlantType, int> skinDic = new();
            foreach (var (key, value) in GameAPP.resourcesManager.plantSkinDic)
            {
                if (CustomCore.CustomPlantsSkin.ContainsKey(key))
                {
                    skinDic.Add(key, value);
                }
            }

            var jsonText = JsonSerializer.Serialize(skinDic);
            var directory = Path.Combine(Application.persistentDataPath, "Skin");
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "skin.json");
            if (!File.Exists(path))
                File.Create(path).Dispose();
            File.WriteAllText(path, jsonText);
        }
    }

    [HarmonyPatch(typeof(AlmanacPlantWindow))]
    public static class AlmanacPlantWindowPatch
    {
        [HarmonyPatch(nameof(AlmanacPlantWindow.SetPlant))]
        [HarmonyPostfix]
        public static void PostInitWindow(AlmanacPlantWindow __instance, ref PlantType thePlantType)
        {
            {
                PlantType plantType = thePlantType;
                if (CustomCore.CustomPlantsSkinActive.ContainsKey(plantType) && !CustomCore.CustomPlantsSkinActive[plantType])
                {
                    if (CustomCore.CustomPlantsSkin.ContainsKey(plantType))
                        __instance.skinButton.SetActive(CustomCore.CustomPlantsSkin.ContainsKey(plantType));
                }
            }
            {
                PlantType plantType = thePlantType;
                if (CustomCore.CustomPlantTypes.Contains(plantType))
                    __instance.skinButton.SetActive(CustomCore.CustomPlantsSkin.ContainsKey(plantType));
            }
            {
                PlantType plantType = thePlantType;
                if (CustomCore.CustomPlantsSkinActive.ContainsKey(plantType) && CustomCore.CustomPlantsSkinActive[plantType]) return;
                String fullName = Directory.GetParent(Application.dataPath)?.FullName;
                if (fullName == null)
                    return;
                string skinPath = Path.Combine(fullName, MelonEnvironment.ModsDirectory, "Skin");
                if (!Directory.Exists(skinPath))
                    return;
                var regex = new Regex($@"^skin_{(int)plantType}(?!\d).*$", RegexOptions.IgnoreCase);
                var files = Directory.GetFiles(skinPath).Where(str => regex.IsMatch(Path.GetFileNameWithoutExtension(str))).ToList();
                __instance.skinButton.SetActive(files.Count > 0);
            }
        }

        [HarmonyPatch(nameof(AlmanacPlantWindow.LeftSkin))]
        [HarmonyPrefix]
        public static void PreLeftSkin(AlmanacPlantWindow __instance, out bool __state)
        {
            __state = __instance.skinButton.active;
        }

        [HarmonyPatch(nameof(AlmanacPlantWindow.LeftSkin))]
        [HarmonyPostfix]
        public static void PostLeftSkin(AlmanacPlantWindow __instance, bool __state)
        {
            __instance.skinButton.SetActive(__state);
        }

        [HarmonyPatch(nameof(AlmanacPlantWindow.RightSkin))]
        [HarmonyPrefix]
        public static void PreRightSkin(AlmanacPlantWindow __instance, out bool __state)
        {
            __state = __instance.skinButton.active;
        }

        [HarmonyPatch(nameof(AlmanacPlantWindow.RightSkin))]
        [HarmonyPostfix]
        public static void PostRightSkin(AlmanacPlantWindow __instance, bool __state)
        {
            __instance.skinButton.SetActive(__state);
        }
    }

    [HarmonyPatch(typeof(AlmanacPlantMenu))]
    public static class AlmanacPlantMenuPatch
    {
        [HarmonyPatch(nameof(AlmanacPlantMenu.InitNameAndInfoFromJson))]
        [HarmonyPostfix]
        public static void PostInitNameAndInfoFromJson()
        {
            foreach (var item in CustomCore.PlantsAlmanac)
            {
                if (AlmanacPlantMenu.PlantAlmanacData.ContainsKey(item.Key)) continue;
                var data = new AlmanacPlantBank.PlantInfo();
                var newName = Regex.Replace(item.Value.Item1, @"\([^()]*\)", "");
                data.name = newName;
                data.info = item.Value.Item2;
                data.seedType = (int)item.Key;
                AlmanacPlantMenu.PlantAlmanacData.Add(item.Key, data);
            }
        }

        [HarmonyPatch(nameof(AlmanacPlantMenu.Awake))]
        [HarmonyPostfix]
        public static void PostAwake(AlmanacPlantMenu __instance)
        {
            var go = __instance.transform.FindChild("FilterMenu/Scroll View/Viewport/Content/Buttons/LookRedCard").gameObject;
            var newSelect = Instantiate(go, __instance.transform.FindChild("FilterMenu/Scroll View/Viewport/Content/Buttons"));
            Action action = () =>
            {
                Func<PlantType, bool> func = (plantType) => !Enum.IsDefined(plantType);
                __instance.ShowPlants(func);
            };
            UnityEvent unityEvent = new();
            unityEvent.AddListener(action);
            newSelect.GetComponent<UIButton>().clickEvent = unityEvent;
            newSelect.name = "LookCustom";
            newSelect.transform.FindChild("TextShadow").gameObject.GetComponent<TextMeshProUGUI>().text = "二创植物";
            newSelect.transform.FindChild("TextShadow/Text").gameObject.GetComponent<TextMeshProUGUI>().text = "二创植物";
            newSelect.transform.localPosition = new Vector3(0f, -44f * newSelect.transform.childCount + 72f, 0f);
        }
    }

    /*[HarmonyPatch(typeof(AlmanacZombieWindow))]
    public static class AlmanacZombieWindowPatch
    {
        [HarmonyPatch(nameof(AlmanacZombieWindow.UpdateText))]
        [HarmonyPostfix]
        public static void PostUpdateText(AlmanacZombieWindow __instance, ref ZombieType theZombieType)
        {
            if (CustomCore.ZombiesAlmanac.ContainsKey(theZombieType))
            {
                var newName = Regex.Replace(CustomCore.ZombiesAlmanac[theZombieType].Item1, @"\([^()]*\)", "");
                foreach (var text in __instance.showedZombieName)
                    text.text = newName;
                __instance.showedZombieIntroduce.text = CustomCore.ZombiesAlmanac[theZombieType].Item2;
            }
        }
    }*/

    [HarmonyPatch(typeof(AlmanacMgrZombie))]
    public static class AlmanacMgrZombiePatch
    {
        [HarmonyPatch("InitNameAndInfoFromJson")]
        [HarmonyPrefix]
        public static bool PreInitNameAndInfoFromJson(AlmanacMgrZombie __instance)
        {
            //若在二创僵尸图鉴列表中
            if (CustomCore.ZombiesAlmanac.ContainsKey(__instance.theZombieType))
            {
                for (int i = 0; i < __instance.transform.childCount; i++)
                {
                    Transform childTransform = __instance.transform.GetChild(i);
                    if (childTransform == null)
                        continue;
                    //僵尸名字
                    if (childTransform.name == "Name")
                    {
                        childTransform.GetComponent<TextMeshPro>().text =
                            CustomCore.ZombiesAlmanac[__instance.theZombieType].Item1;
                        childTransform.GetChild(0).GetComponent<TextMeshPro>().text =
                            CustomCore.ZombiesAlmanac[__instance.theZombieType].Item1;
                    }
                    //僵尸信息
                    if (childTransform.name == "Info")
                    {
                        TextMeshPro info = childTransform.GetComponent<TextMeshPro>();
                        info.overflowMode = TextOverflowModes.Page;
                        info.fontSize = 40;
                        info.text = CustomCore.ZombiesAlmanac[__instance.theZombieType].Item2;
                        __instance.introduce = info;
                    }

                    if (childTransform.name == "Cost")
                        childTransform.GetComponent<TextMeshPro>().text = "";
                }

                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(ConveyManager))]
    public static class ConveyBeltMgrPatch
    {
        [HarmonyPatch(nameof(ConveyManager.Awake))]
        [HarmonyPostfix]
        public static void PostAwake(ConveyManager __instance)
        {
            if (Utils.IsCustomLevel(out var levelData) && levelData.BoardTag.isConvey && levelData.ConveyBeltPlantTypes().Count > 0)
            {
                __instance.plants = levelData.ConveyBeltPlantTypes().ToIl2CppList();
            }
        }

        [HarmonyPatch(nameof(ConveyManager.GetCardPool))]
        [HarmonyPostfix]
        public static void PostGetCardPool(ref Il2CppSystem.Collections.Generic.List<PlantType> __result)
        {
            if (Utils.IsCustomLevel(out var levelData) && levelData.BoardTag.isConvey && levelData.ConveyBeltPlantTypes().Count > 0)
            {
                __result = levelData.ConveyBeltPlantTypes().ToIl2CppList();
            }
        }
    }

    [HarmonyPatch(typeof(InGameText), nameof(InGameText.ShowText))]
    public static class InGameTextPatch
    {
        public static bool disable = false;
        [HarmonyPrefix]
        public static bool Prefix(string text, float time)
        {
            if (text == "通关挑战模式解锁配方" && time == 7f && disable)
            {
                disable = false;
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// 为二创植物附加植物特性
    /// </summary>
    [HarmonyPatch(typeof(CreatePlant))]
    public static class CreatePlantPatch
    {
        [HarmonyPatch(nameof(CreatePlant.SetPlant))]
        [HarmonyPostfix]
        public static void Postfix_SetPlant(CreatePlant __instance, ref GameObject __result)
        {
            if (__result != null && __result.TryGetComponent<Plant>(out var plant) &&
                CustomCore.CustomPlantTypes.Contains(plant.thePlantType))
            {
                TypeMgr.GetPlantTag(plant);
            }
        }

        [HarmonyPatch(nameof(CreatePlant.Lim))]
        [HarmonyPostfix]
        public static void PostLim(CreatePlant __instance, ref PlantType theSeedType, ref bool __result)
        {
            // 自定义条件
            {
                if (CustomCore.CustomFailMix.ContainsKey(theSeedType) && CustomCore.CustomFailMix[theSeedType].Item1 != null)
                {
                    if (CustomCore.CustomFailMix[theSeedType].Item1.Invoke())
                        CustomCore.CustomFailMix[theSeedType].Item2?.Invoke();
                    else
                    {
                        __result = true;
                        InGameTextPatch.disable = true;
                        CustomCore.CustomFailMix[theSeedType].Item3?.Invoke();
                    }
                }
            }
        }

        [HarmonyPatch(nameof(CreatePlant.LimTravel))]
        [HarmonyPostfix]
        public static void Postfix_LimTravel(CreatePlant __instance, ref PlantType theSeedType, ref bool __result)
        {
            // 判定
            {
                bool isCanSet = false;
                if (TravelMgr.Instance != null && TravelMgr.Instance.ulockTemp.Contains(theSeedType))
                    isCanSet = true;
                if (__instance.board.boardTag.enableAllTravelPlant || __instance.board.boardTag.enableTravelPlant || __instance.board.boardTag.isTravel || __instance.board.boardTag.isSuperRandom || __instance.board.boardTag.isUltimateSuperRandom)
                    isCanSet = true;

                if (CustomCore.CustomUltimatePlants.Contains(theSeedType) && !isCanSet)
                {
                    __result = true;
                    InGameText.Instance.ShowText("该配方仅旅行生存系列或深渊可用", 3f, false);
                }
            }
            // 弱究
            {
                if (CustomCore.CustomWeakUltimatePlants.Contains(theSeedType))
                {
                    if (__instance.board == null)
                        __result = false;
                    else
                    {
                        if (!__instance.board.boardTag.enableAllTravelPlant && !__instance.board.boardTag.enableTravelPlant && !__instance.board.boardTag.isSuperRandom && !__instance.board.boardTag.isUltimateSuperRandom)
                        {
                            __result = true;
                            InGameText.Instance.ShowText("该配方仅旅行模式或深渊可用", 3f);
                        }
                        else
                        {
                            if (TravelMgr.Instance == null)
                                __result = false;
                            else
                            {
                                var mgr = TravelMgr.Instance;
                                if (!(__instance.board.boardTag.isTravel && mgr.ulockTemp != null && mgr.ulockTemp.Contains(theSeedType) || 
                                    (mgr.weakUltimates != null && mgr.weakUltimates.Contains(theSeedType))) && !__instance.board.boardTag.enableAllTravelPlant)
                                {
                                    __result = true;
                                    InGameText.Instance.ShowText("未选取此植物", 3f);
                                }
                                else
                                {
                                    bool unlock = false;
                                    int index = (int)theSeedType;

                                    if (mgr.unlockPlant != null && index >= 0 && index < mgr.unlockPlant.Length)
                                        unlock = mgr.unlockPlant[index];
                                    if (unlock)
                                    {
                                        __result = true;
                                        InGameText.Instance.ShowText("该配方仅旅行模式或深渊可用", 4f);
                                    }
                                    else
                                        __result = false;
                                }
                            }
                        }
                    }
                }
            }
            // 强究
            {
                if (CustomCore.CustomStrongUltimatePlants.ContainsKey(theSeedType))
                {
                    if (__instance.board == null)
                        __result = false;
                    else
                    {
                        if (!__instance.board.boardTag.enableAllTravelPlant && !__instance.board.boardTag.enableTravelPlant && !__instance.board.boardTag.isSuperRandom && !__instance.board.boardTag.isUltimateSuperRandom)
                        {
                            __result = true;
                            InGameText.Instance.ShowText("该配方仅旅行模式或深渊可用", 4f);
                        }
                        else
                        {
                            if (TravelMgr.Instance == null)
                                __result = false;
                            else
                            {
                                if (TravelMgr.Instance.unlockPlant[CustomCore.CustomStrongUltimatePlants[theSeedType]] || __instance.board.boardTag.enableAllTravelPlant || __instance.board.boardTag.isSuperRandom || __instance.board.boardTag.isUltimateSuperRandom)
                                    __result = false;
                                else
                                {
                                    __result = true;
                                    InGameText.Instance.ShowText("该配方需要抽取", 4f);
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(nameof(CreatePlant.MixBombCheck))]
        [HarmonyPrefix]
        public static bool Prefix_MixBombCheck(CreatePlant __instance, ref int theBoxColumn, ref int theBoxRow, ref bool __result)
        {
            List<Plant> plants = Lawnf.Get1x1Plants(theBoxColumn, theBoxRow).ToArray().ToList();
            foreach (var plant in plants)
            {
                if (plant == null) continue;
                if (CustomCore.CustomMixBombFusions.Any(kvp => kvp.Key.Item2 == plant.thePlantType))
                {
                    __result = true;
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Lawnf))]
    public class LawnfPatch
    {
        [HarmonyPatch(nameof(Lawnf.GetUpgradedPlantCost))]
        [HarmonyPrefix]
        public static bool Prefix(ref PlantType thePlantType, ref int targetLevel, ref int __result)
        {
            if (CustomCore.CustomUltimatePlants.Contains(thePlantType))
            {
                __result = 1500 * (targetLevel) * (targetLevel + 1) / 2;
                return false;
            }
            return true;
        }

        [HarmonyPatch(nameof(Lawnf.IsUltiPlant))]
        [HarmonyPrefix]
        public static bool Prefix(ref PlantType thePlantType, ref bool __result)
        {
            if (CustomCore.CustomPlantTypes.Contains(thePlantType))
            {
                __result = CustomCore.CustomUltimatePlants.Contains(thePlantType);
                return false;
            }
            return true;
        }

        [HarmonyPatch(nameof(Lawnf.GetUltimatePlants))]
        [HarmonyPostfix]
        public static void Postfix(ref Il2CppSystem.Collections.Generic.List<PlantType> __result)
        {
            foreach (PlantType plantType in CustomCore.CustomUltimatePlants)
            {
                if (!__result.Contains(plantType))
                {
                    __result.Add(plantType);
                }
            }
        }

        [HarmonyPatch(nameof(Lawnf.TravelAdvanced), new Type[] { typeof(AdvBuff) })]
        [HarmonyPostfix]
        public static void PostTravelAdvanced_0(ref AdvBuff buff, ref bool __result)
        {
            var result = Utils.IsMultiLevelBuff(BuffType.AdvancedBuff, (int)buff);
            if (!result.Item1)
                return;
            int index = result.Item2;
            if (TravelMgr.Instance == null)
                return;
            var array = TravelMgr.Instance.GetData<int[]>("CustomBuffsLevel");
            if (array is null)
                return;
            __result = array[index] > 0;
        }

        [HarmonyPatch(nameof(Lawnf.TravelAdvanced), new Type[] { typeof(int) })]
        [HarmonyPostfix]
        public static void PostTravelAdvanced_1(ref int i, ref bool __result)
        {
            var result = Utils.IsMultiLevelBuff(BuffType.AdvancedBuff, i);
            if (!result.Item1)
                return;
            int index = result.Item2;
            if (TravelMgr.Instance == null)
                return;
            var array = TravelMgr.Instance.GetData<int[]>("CustomBuffsLevel");
            if (array is null)
                return;
            __result = array[index] > 0;
        }

        [HarmonyPatch(nameof(Lawnf.TravelUltimate), new Type[] { typeof(UltiBuffs) })]
        [HarmonyPostfix]
        public static void PostTravelUltimate_0(ref UltiBuffs i, ref bool __result)
        {
            var result = Utils.IsMultiLevelBuff(BuffType.UltimateBuff, (int)i);
            if (!result.Item1)
                return;
            int index = result.Item2;
            if (TravelMgr.Instance == null)
                return;
            var array = TravelMgr.Instance.GetData<int[]>("CustomBuffsLevel");
            if (array is null)
                return;
            __result = array[index] > 0;
        }

        [HarmonyPatch(nameof(Lawnf.TravelUltimate), new Type[] { typeof(int) })]
        [HarmonyPostfix]
        public static void PostTravelUltimate_1(ref int i, ref bool __result)
        {
            var result = Utils.IsMultiLevelBuff(BuffType.UltimateBuff, i);
            if (!result.Item1)
                return;
            int index = result.Item2;
            if (TravelMgr.Instance == null)
                return;
            var array = TravelMgr.Instance.GetData<int[]>("CustomBuffsLevel");
            if (array is null)
                return;
            __result = array[index] > 0;
        }

        [HarmonyPatch(nameof(Lawnf.TravelUltimateLevel))]
        [HarmonyPostfix]
        public static void PostTravelUltimateLevel(ref int index, ref int __result)
        {
            var result = Utils.IsMultiLevelBuff(BuffType.UltimateBuff, index);
            if (!result.Item1)
                return;
            int index2 = result.Item2;
            if (TravelMgr.Instance == null)
                return;
            var array = TravelMgr.Instance.GetData<int[]>("CustomBuffsLevel");
            if (array is null)
                return;
            __result = array[index2];
        }

        [HarmonyPatch(nameof(Lawnf.TravelDebuff), new Type[] { typeof(int) })]
        [HarmonyPostfix]
        public static void PostTravelDebuff_0(ref int i, ref bool __result)
        {
            var result = Utils.IsMultiLevelBuff(BuffType.Debuff, i);
            if (!result.Item1)
                return;
            int index = result.Item2;
            if (TravelMgr.Instance == null)
                return;
            var array = TravelMgr.Instance.GetData<int[]>("CustomBuffsLevel");
            if (array is null)
                return;
            __result = array[index] > 0;
        }

        [HarmonyPatch(nameof(Lawnf.TravelDebuff), new Type[] { typeof(TravelDebuff) })]
        [HarmonyPostfix]
        public static void PostTravelDebuff_1(ref TravelDebuff travelDebuff, ref bool __result)
        {
            var result = Utils.IsMultiLevelBuff(BuffType.Debuff, (int)travelDebuff);
            if (!result.Item1)
                return;
            int index = result.Item2;
            if (TravelMgr.Instance == null)
                return;
            var array = TravelMgr.Instance.GetData<int[]>("CustomBuffsLevel");
            if (array is null)
                return;
            __result = array[index] > 0;
        }
    }

    [HarmonyPatch(typeof(Lawnf))]
    public static class LawnfPatch_BuffGet
    { 
        [HarmonyPatch(nameof(Lawnf.TravelAdvanced), new Type[] { typeof(int) })]
        [HarmonyPrefix]
        public static void PreTravelAdvanced_0(ref int i)
        {
            if (CustomCore.CustomBuffIDMapping.ContainsKey((BuffType.AdvancedBuff, i)))
                i = CustomCore.CustomBuffIDMapping[(BuffType.AdvancedBuff, i)];
        }

        [HarmonyPatch(nameof(Lawnf.TravelAdvanced), new Type[] { typeof(AdvBuff) })]
        [HarmonyPrefix]
        public static void PreTravelAdvanced_1(ref AdvBuff buff)
        {
            if (CustomCore.CustomBuffIDMapping.ContainsKey((BuffType.AdvancedBuff, (int)buff)))
                buff = (AdvBuff)CustomCore.CustomBuffIDMapping[(BuffType.AdvancedBuff, (int)buff)];
        }

        [HarmonyPatch(nameof(Lawnf.TravelUltimate), new Type[] { typeof(int) })]
        [HarmonyPrefix]
        public static void PreTravelUltimate_0(ref int i)
        {
            if (CustomCore.CustomBuffIDMapping.ContainsKey((BuffType.UltimateBuff, i)))
                i = CustomCore.CustomBuffIDMapping[(BuffType.UltimateBuff, i)];
        }

        [HarmonyPatch(nameof(Lawnf.TravelUltimate), new Type[] { typeof(UltiBuffs) })]
        [HarmonyPrefix]
        public static void PreTravelUltimate_1(ref UltiBuffs i)
        {
            if (CustomCore.CustomBuffIDMapping.ContainsKey((BuffType.UltimateBuff, (int)i)))
                i = (UltiBuffs)CustomCore.CustomBuffIDMapping[(BuffType.UltimateBuff, (int)i)];
        }

        [HarmonyPatch(nameof(Lawnf.TravelDebuff), new Type[] { typeof(int) })]
        [HarmonyPrefix]
        public static void PreTravelDebuff_0(ref int i)
        {
            if (CustomCore.CustomBuffIDMapping.ContainsKey((BuffType.Debuff, i)))
                i = CustomCore.CustomBuffIDMapping[(BuffType.Debuff, i)];
        }

        [HarmonyPatch(nameof(Lawnf.TravelDebuff), new Type[] { typeof(TravelDebuff) })]
        [HarmonyPrefix]
        public static void PreTravelDebuff_1(ref TravelDebuff travelDebuff)
        {
            if (CustomCore.CustomBuffIDMapping.ContainsKey((BuffType.Debuff, (int)travelDebuff)))
                travelDebuff = (TravelDebuff)CustomCore.CustomBuffIDMapping[(BuffType.Debuff, (int)travelDebuff)];
        }
    }

    [HarmonyPatch(typeof(GameAPP), nameof(GameAPP.LoadResources))]
    public static class GameAPPPatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            #region 自动扩容
            // 扩容plantData
            if (CustomCore.CustomPlants.Count > 0 && (int)CustomCore.CustomPlants.Keys.Max() + 1 >= PlantDataLoader.plantData.Length)
            {
                long size_plantData = (int)CustomCore.CustomPlants.Keys.Max();
                Il2CppReferenceArray<PlantDataLoader.PlantData_> plantData = new Il2CppReferenceArray<PlantDataLoader.PlantData_>(size_plantData + 1);
                PlantDataLoader.plantData = plantData;
            }

            // 扩容particlePrefab
            if (CustomCore.CustomParticles.Count > 0 && (int)CustomCore.CustomParticles.Keys.Max() + 1 >= GameAPP.particlePrefab.Length)
            {
                long size_particlePrefab = (int)CustomCore.CustomParticles.Keys.Max();
                Il2CppReferenceArray<GameObject> particlePrefab = new Il2CppReferenceArray<GameObject>(size_particlePrefab + 1);
                GameAPP.particlePrefab = particlePrefab;
            }

            // 扩容spritePrefab
            if (CustomCore.CustomSprites.Count > 0 && CustomCore.CustomSprites.Keys.Max() + 1 >= GameAPP.spritePrefab.Length)
            {
                long size_spritePrefab = CustomCore.CustomSprites.Keys.Max();
                Il2CppReferenceArray<Sprite> spritePrefab = new Il2CppReferenceArray<Sprite>(size_spritePrefab + 1);
                GameAPP.spritePrefab = spritePrefab;
            }

            // 扩容data融合数组
            if (CustomCore.CustomPlants.Count > 0 && ((long)CustomCore.CustomPlants.Keys.Max() + 1 >= MixData.data.Cast<Il2CppSystem.Array>().GetLongLength(0) || (long)CustomCore.CustomPlants.Keys.Max() + 1 >= MixData.data.Cast<Il2CppSystem.Array>().GetLongLength(1)))
            {
                var arr = MixData.data.Cast<Il2CppSystem.Array>();
                long max = (long)CustomCore.CustomPlants.Keys.Max() + 1;
                var type = arr.GetValue(0, 0).GetIl2CppType();
                var result = Il2CppSystem.Array.CreateInstance(type, max, max);
                MixData.data = result;
            }

            // 扩容disMixDatas拆分数组
            if (CustomCore.CustomPlants.Count > 0 && (int)CustomCore.CustomPlants.Keys.Max() + 1 >= MixData.disMixDatas.Length)
            {
                long size_disMixDatas = (int)CustomCore.CustomPlants.Keys.Max();
                Il2CppReferenceArray<MixData.DisMixData> disMixDatas = new Il2CppReferenceArray<MixData.DisMixData>(size_disMixDatas + 1);
                MixData.disMixDatas = disMixDatas;
            }

            // 扩容randomData随机融合数组
            if (CustomCore.CustomPlants.Count > 0 && ((long)CustomCore.CustomPlants.Keys.Max() + 1 >= MixData.randomData.Cast<Il2CppSystem.Array>().GetLongLength(0) || (long)CustomCore.CustomPlants.Keys.Max() + 1 >= MixData.randomData.Cast<Il2CppSystem.Array>().GetLongLength(1)))
            {
                var arr = MixData.randomData.Cast<Il2CppSystem.Array>();
                long max = (int)CustomCore.CustomPlants.Keys.Max() + 1;
                var type = arr.GetValue(0, 0).GetIl2CppType();
                var result = Il2CppSystem.Array.CreateInstance(type, max, max);
                MixData.randomData = result;
            }
            #endregion
            foreach (var plant in CustomCore.CustomPlants)//二创植物
            {
                GameAPP.resourcesManager.plantPrefabs[plant.Key] = plant.Value.Prefab;//注册预制体
                GameAPP.resourcesManager.plantPrefabs[plant.Key].tag = "Plant";//必须打tag
                if (!GameAPP.resourcesManager.allPlants.Contains(plant.Key))
                    GameAPP.resourcesManager.allPlants.Add(plant.Key);//注册植物类型
                if (plant.Value.PlantData is not null)
                {
                    PlantDataLoader.plantData[(int)plant.Key] = plant.Value.PlantData;//注册植物数据
                    PlantDataLoader.plantDatas.Add(plant.Key, plant.Value.PlantData);
                }
                GameAPP.resourcesManager.plantPreviews[plant.Key] = plant.Value.Preview;//注册植物预览
                GameAPP.resourcesManager.plantPreviews[plant.Key].tag = "Preview";//必修打tag
            }
            Il2CppSystem.Array array = MixData.data.Cast<Il2CppSystem.Array>();//注册融合配方
            foreach (var f in CustomCore.CustomFusions)
            {
                array.SetValue(f.Item1, f.Item2, f.Item3);
            }

            foreach (var z in CustomCore.CustomZombies)//注册二创僵尸
            {
                if (!GameAPP.resourcesManager.allZombieTypes.Contains(z.Key))
                    GameAPP.resourcesManager.allZombieTypes.Add(z.Key);//注册僵尸类型
                GameAPP.resourcesManager.zombiePrefabs[z.Key] = z.Value.Item1;//注册僵尸预制体
                GameAPP.resourcesManager.zombiePrefabs[z.Key].tag = "Zombie";//必修打tag
            }

            foreach (var bullet in CustomCore.CustomBullets)//注册二创子弹
            {
                GameAPP.resourcesManager.bulletPrefabs[bullet.Key] = bullet.Value;//注册子弹预制体
                if (!GameAPP.resourcesManager.allBullets.Contains(bullet.Key))
                    GameAPP.resourcesManager.allBullets.Add(bullet.Key);//注册子弹类型
            }

            foreach (var par in CustomCore.CustomParticles)//注册粒子效果
            {
                GameAPP.particlePrefab[(int)par.Key] = par.Value;
                GameAPP.resourcesManager.particlePrefabs[par.Key] = par.Value;//注册粒子效果预制体
                if (!GameAPP.resourcesManager.allParticles.Contains(par.Key))
                    GameAPP.resourcesManager.allParticles.Add(par.Key);//注册粒子效果类型
            }

            foreach (var spr in CustomCore.CustomSprites)//注册自定义精灵贴图
            {
                GameAPP.spritePrefab[spr.Key] = spr.Value;
            }
        }
    }

    /// <summary>
    /// 资源加载
    /// </summary>
    [HarmonyPatch(typeof(NoticeMenu), nameof(NoticeMenu.Start))]
    public static class NoticeMenuPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            // 注册红卡
            {
                var propertyInfo = typeof(TypeMgr).GetProperty("RedPlant", BindingFlags.Static | BindingFlags.Public);
                if (propertyInfo is null)
                    goto Lable1;
                var value = propertyInfo.GetValue(null);
                if (value is null)
                    goto Lable1;
                var redPlant = (Il2CppSystem.Collections.Generic.HashSet<PlantType>)value;
                foreach (var (k, v) in CustomCore.TypeMgrExtra.LevelPlants)
                    if (v == CardLevel.Red)
                        redPlant.Add(k);
                propertyInfo.SetValue(null, redPlant);
            }
        Lable1:
            // 注册防碾压植物
            {
                var propertyInfo = typeof(TypeMgr).GetProperty("UncrashablePlants", BindingFlags.Static | BindingFlags.Public);
                if (propertyInfo is null)
                    return;
                var value = propertyInfo.GetValue(null);
                if (value is null)
                    return;
                var uncrashablePlants = (Il2CppSystem.Collections.Generic.HashSet<PlantType>)value;
                foreach (var item in CustomCore.TypeMgrExtra.UncrashablePlants)
                    uncrashablePlants.Add(item);
                propertyInfo.SetValue(null, uncrashablePlants);
            }

            #region 注册皮肤
            foreach (var item in CustomCore.CustomPlantsSkin)
            {
                var plantType = item.Key;
                if (!CustomCore.CustomPlantsSkinActive[plantType])
                {
                    if (!GameAPP.resourcesManager.plantSkinDic.TryGetValue(plantType, out var _))
                        GameAPP.resourcesManager.plantSkinDic.Add(plantType, 0);
                    foreach (var it in item.Value)
                    {
                        var prefab = it.Prefab;
                        var preview = it.Preview;

                        if (prefab != null)
                        {
                            if (GameAPP.resourcesManager._plantPrefabs.ContainsKey(plantType))
                                GameAPP.resourcesManager._plantPrefabs[plantType].Add(prefab);
                            else
                            {
                                Il2CppSystem.Collections.Generic.List<GameObject> list = new();
                                list.Add(GameAPP.resourcesManager.plantPrefabs[plantType]);
                                list.Add(prefab);
                                GameAPP.resourcesManager._plantPrefabs.Add(plantType, list);
                            }
                        }
                        if (preview != null)
                        {
                            if (GameAPP.resourcesManager._plantPreviews.ContainsKey(plantType))
                                GameAPP.resourcesManager._plantPreviews[plantType].Add(preview);
                            else
                            {
                                Il2CppSystem.Collections.Generic.List<GameObject> list = new();
                                list.Add(GameAPP.resourcesManager.plantPreviews[plantType]);
                                list.Add(preview);
                                GameAPP.resourcesManager._plantPreviews.Add(plantType, list);
                            }
                        }
                        CustomCore.CustomPlantsSkinActive[plantType] = true;
                    }
                }
            }
            String? fullName = Directory.GetParent(Application.dataPath)?.FullName;
            if (fullName != null)
            {
                string skinPath = Path.Combine(MelonEnvironment.ModsDirectory, "Skin");
                if (Directory.Exists(skinPath))
                {
                    var regex = new Regex(@"^skin_(\d+)(?!\d).*$", RegexOptions.IgnoreCase);
                    foreach (var path in Directory.GetFiles(skinPath))
                    {
                        var match = regex.Match(Path.GetFileNameWithoutExtension(path));
                        if (match.Success && int.TryParse(match.Groups[1].Value, out int id))
                        {
                            var plantType = (PlantType)id;
                            if (CustomCore.CustomPlantsSkinActive.ContainsKey(plantType) && CustomCore.CustomPlantsSkinActive[plantType]) continue;
                            var ab = AssetBundle.LoadFromFile(path);
                            GameObject? prefab = null;
                            GameObject? preview = null;
                            try
                            {
                                prefab = ab.GetAsset<GameObject>("Prefab");
                                prefab.tag = "Plant";
                            }
                            catch { continue; }
                            try
                            {
                                preview = ab.GetAsset<GameObject>("Preview");
                                preview.tag = "Preview";
                            }
                            catch { continue; }
                            CustomPlantData data = new()
                            {
                                ID = id,
                                PlantData = PlantDataLoader.plantDatas[plantType],
                                Prefab = GameAPP.resourcesManager.plantPrefabs[plantType],
                                Preview = GameAPP.resourcesManager.plantPreviews[plantType]
                            };

                            if (!GameAPP.resourcesManager.plantSkinDic.TryGetValue(plantType, out var _))
                                GameAPP.resourcesManager.plantSkinDic.Add(plantType, 0);

                            if (prefab != null)
                            {
                                foreach (var comp in GameAPP.resourcesManager.plantPrefabs[plantType].GetComponents<Component>())
                                    if (!prefab.TryGetComponent(comp.GetIl2CppType(), out var cmp) && cmp == null)
                                        prefab.AddComponent(comp.GetIl2CppType());
                                prefab.GetComponent<Plant>().thePlantType = plantType;

                                if (GameAPP.resourcesManager._plantPrefabs.ContainsKey(plantType))
                                    GameAPP.resourcesManager._plantPrefabs[plantType].Add(prefab);
                                else
                                {
                                    Il2CppSystem.Collections.Generic.List<GameObject> list = new();
                                    list.Add(GameAPP.resourcesManager.plantPrefabs[plantType]);
                                    list.Add(prefab);
                                    GameAPP.resourcesManager._plantPrefabs.Add(plantType, list);
                                }

                                prefab.GetComponent<Plant>().FindShoot(prefab.GetComponent<Plant>().transform);
                                data.Prefab = prefab;
                            }

                            if (preview != null)
                            {
                                foreach (var comp in GameAPP.resourcesManager.plantPreviews[plantType].GetComponents<Component>())
                                    if (!preview.TryGetComponent(comp.GetIl2CppType(), out var cmp) && cmp == null)
                                        preview.AddComponent(comp.GetIl2CppType());

                                if (GameAPP.resourcesManager._plantPreviews.ContainsKey(plantType))
                                    GameAPP.resourcesManager._plantPreviews[plantType].Add(preview);
                                else
                                {
                                    Il2CppSystem.Collections.Generic.List<GameObject> list = new();
                                    list.Add(GameAPP.resourcesManager.plantPreviews[plantType]);
                                    list.Add(preview);
                                    GameAPP.resourcesManager._plantPreviews.Add(plantType, list);
                                }

                                data.Preview = preview;
                            }
                            if (CustomCore.CustomPlantsSkin.ContainsKey(plantType))
                                CustomCore.CustomPlantsSkin[plantType].Add(data);
                            else
                                CustomCore.CustomPlantsSkin.Add(plantType, new List<CustomPlantData> { data });
                        }
                    }
                }
            }

            // 读取存档的皮肤
            {
                var directory = Path.Combine(Application.persistentDataPath, "Skin");
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                var path = Path.Combine(directory, "skin.json");
                if (!File.Exists(path))
                {
                    File.Create(path).Dispose();
                }
                else
                {
                    var content = File.ReadAllText(path);
                    try
                    {
                        var skinDic = JsonSerializer.Deserialize<Dictionary<PlantType, int>>(content);
                        if (skinDic != null)
                        {
                            foreach (var (key, value) in skinDic)
                            {
                                if (GameAPP.resourcesManager.plantSkinDic.ContainsKey(key))
                                {
                                    if (GameAPP.resourcesManager._plantPrefabs.ContainsKey(key) && GameAPP.resourcesManager._plantPrefabs[key].Count > value &&
                                        GameAPP.resourcesManager._plantPreviews.ContainsKey(key) && GameAPP.resourcesManager._plantPreviews[key].Count > value)
                                    {
                                        GameAPP.resourcesManager.plantPrefabs[key] = GameAPP.resourcesManager._plantPrefabs[key][value];
                                        GameAPP.resourcesManager.plantPreviews[key] = GameAPP.resourcesManager._plantPreviews[key][value];
                                        GameAPP.resourcesManager.plantSkinDic[key] = value;
                                    }
                                    else
                                    {
                                        try
                                        {
                                            GameAPP.resourcesManager.plantPrefabs[key] = GameAPP.resourcesManager._plantPrefabs[key][0];
                                            GameAPP.resourcesManager.plantPreviews[key] = GameAPP.resourcesManager._plantPreviews[key][0];
                                            GameAPP.resourcesManager.plantSkinDic[key] = 0;
                                        }
                                        catch (Exception) { }
                                    }
                                }
                                else
                                    continue;
                            }
                        }
                    }
                    catch (JsonException) { }
                }
            }
            #endregion

            if (ElementUpgrade._upgradeCache != null)
            {
                ElementUpgrade._upgradeCache = null;
                ElementUpgrade.InitializeUpgradeCache();
            }
        }
    }

    /// <summary>
    /// 点击其他Button，隐藏二创植物界面
    /// </summary>
    [HarmonyPatch(typeof(UIButton))]
    public static class HideCustomPlantCards
    {
        [HarmonyPatch(nameof(UIButton.OnMouseUpAsButton))]
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (SelectCustomPlants.MyPageParent != null && SelectCustomPlants.MyPageParent.active && GameAPP.theGameStatus != GameStatus.BigGarden)
                SelectCustomPlants.MyPageParent.SetActive(false);
        }

        [HarmonyPatch(nameof(UIButton.Start))]
        [HarmonyPostfix]
        public static void PostfixStart(UIButton __instance)
        {
            if (__instance.name == "LastPage" && Board.Instance != null && Board.Instance.isIZ)
            {
                SelectCustomPlants.InitCustomCards_IZ();
            }
        }
    }

    [HarmonyPatch(typeof(InGameUI))]
    public static class InGameUIPatch
    {
        [HarmonyPatch(nameof(InGameUI.SetUniqueText))]
        [HarmonyPostfix]
        public static void PostSetUniqueText(InGameUI __instance, ref Il2CppReferenceArray<TextMeshProUGUI> T)
        {
            if (GameAPP.theBoardType is (LevelType)66)
            {
                __instance.ChangeString(T, CustomCore.CustomLevels[GameAPP.theBoardLevel].Name());
            }
        }

        [HarmonyPatch(nameof(InGameUI.MoveCard))]
        [HarmonyPrefix]
        public static void PreMoveCard(ref CardUI card)
        {
            foreach (CheckCardState check in CustomCore.checkBehaviours)
            {
                if (check != null)
                {
                    check.movingCardUI = card;
                    check.CheckState();
                }
            }
        }

        [HarmonyPatch(nameof(InGameUI.RemoveCardFromBank))]
        [HarmonyPostfix]
        public static void PostReMoveCardFromBank(ref CardUI card)
        {
            foreach (CheckCardState check in CustomCore.checkBehaviours)
            {
                if (check != null)
                {
                    check.movingCardUI = card;
                    check.CheckState();
                }
            }
        }
    }

    [HarmonyPatch(typeof(InitBoard))]
    public static class InitBoardPatch
    {
        [HarmonyPatch(nameof(InitBoard.PreSelectCard))]
        [HarmonyPostfix]
        public static void PostPreSelectCard(InitBoard __instance)
        {
            if (GameAPP.theBoardType is (LevelType)66)
            {
                foreach (var c in CustomCore.CustomLevels[GameAPP.theBoardLevel].PreSelectCards())
                {
                    __instance.PreSelect(c);
                }
            }
        }

        [HarmonyPatch(nameof(InitBoard.RightMoveCamera))]
        [HarmonyPostfix]
        public static void PostRightMoveCamera()
        {
            if (!Utils.IsCustomLevel(out _)) return;
            var levelData = CustomCore.CustomLevels[GameAPP.theBoardLevel];
            var travelMgr = GameAPP.gameAPP.GetOrAddComponent<TravelMgr>();
            foreach (var a in levelData.AdvBuffs())
            {
                if (a >= 0 && a < travelMgr.advancedUpgrades.Count)
                {
                    travelMgr.advancedUpgrades[a] = true;
                }
            }
            foreach (var u in levelData.UltiBuffs())
            {
                if (u.Item1 >= 0 && u.Item1 < travelMgr.ultimateUpgrades.Count && u.Item2 >= 0)
                {
                    travelMgr.ultimateUpgrades[u.Item1] = u.Item2;
                }
            }
            foreach (var p in levelData.UnlockPlants())
            {
                if (p >= 0 && p < travelMgr.unlockPlant.Count)
                {
                    travelMgr.unlockPlant[p] = true;
                }
            }
            foreach (var d in levelData.Debuffs())
            {
                if (d >= 0 && d < travelMgr.debuff.Count)
                {
                    travelMgr.debuff[d] = true;
                }
            }
        }

        [HarmonyPatch(nameof(InitBoard.MoveOverEvent))]
        [HarmonyPrefix]
        public static bool PreMoveOverEvent(InitBoard __instance, ref string direction)
        {
            if (!Utils.IsCustomLevel(out var levelData)) return true;
            if (direction == "right")
            {
                if (__instance.board != null)
                {
                    if (__instance.board.cardSelectable)
                    {
                        // 设置游戏状态
                        GameAPP.theGameStatus = GameStatus.Selecting;

                        // UI控制
                        InGameUI.Instance.ConveyorBelt.SetActive(false);
                        InGameUI.Instance.Bottom.SetActive(true);

                        // 启动协程移动UI元素
                        __instance.StartCoroutine(__instance.MoveDirection(InGameUI.Instance.SeedBank, 79f, 0));
                        __instance.StartCoroutine(__instance.MoveDirection(InGameUI.Instance.Bottom, 525f, 1));
                    }
                    else
                    {
                        // 延迟执行方法
                        __instance.Invoke("LeftMoveCamera", 1.5f);
                        InGameUI.Instance.Bottom.SetActive(false);
                    }
                }
            }
            else if (direction == "left")
            {
                if (__instance.board == null) return false;

                if (!__instance.board.cardSelectable)
                {
                    if (__instance.board.cardBank)
                    {
                        __instance.StartCoroutine(__instance.MoveDirection(InGameUI.Instance.SeedBank, 79f, 0));
                        __instance.AddCard();
                    }
                    else
                    {
                        InGameUI.Instance.SeedBank.SetActive(false);
                    }
                    InGameUI.Instance.Bottom.SetActive(false);
                }

                // 音量渐变协程
                __instance.StartCoroutine(__instance.DecreaseVolume());

                // 降低UI位置
                InGameUI.Instance.LowerUI();

                // 初始化割草机（特定模式下）
                if (!__instance.board.boardTag.disableMower)
                {
                    __instance.InitMower();
                }

                // 雾效果移动
                if (__instance.board.fog != null)
                {
                    Vector3 fogPosition = __instance.board.fog.transform.position;
                    Vector3 boardPosition = __instance.board.background.transform.position;

                    FogMgr.Instance.MoveObject(
                        new(fogPosition.x,
                        fogPosition.y,
                        boardPosition.z),
                        10f  // 移动速度
                    );
                }

                // BOSS战特殊处理
                float invokeDelay = 0.5f;
                if (__instance.board.boardTag.isBoss || __instance.board.boardTag.isBoss2)
                {
                    GameObject zombie = CreateZombie.Instance.SetZombie(0, levelData.RealBoss2 ? ZombieType.ZombieBoss2 : ZombieType.ZombieBoss, 0f);
                    Zombie zombieComp = zombie.GetComponent<Zombie>();

                    if (__instance.board.boss2)
                    {
                        Lawnf.SetZombieHealth(zombieComp, 5f);
                    }
                    invokeDelay = 3.5f;
                    __instance.board.boss2 = levelData.RealBoss2;
                }

                // 延迟调用方法
                __instance.Invoke("ReadySetPlant", invokeDelay);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(InitZombieList))]
    public static class InitZombieListPatch
    {
        [HarmonyPatch(nameof(InitZombieList.SetAllowZombieTypeSpawn))]
        [HarmonyPrefix]
        public static void PostInitZombie(ref LevelType theLevelType, ref int theLevelNumber)
        {
            if (Utils.IsCustomLevel(out var levelData))
            {
                Il2CppSystem.Collections.Generic.List<ZombieType> list = new();
                foreach (var z in levelData.ZombieList())
                    list.Add(z);
                InitZombieList.AllowZombies(list);
            }
        }
    }

    /// <summary>
    /// 花钱开大招
    /// </summary>
    [HarmonyPatch(typeof(Money))]
    public static class MoneyPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("ReinforcePlant")]
        public static bool PreReinforcePlant(Money __instance, ref Plant plant)
        {
            if (CustomCore.SuperSkills.ContainsKey(plant.thePlantType))
            {
                var cost = CustomCore.SuperSkills[plant.thePlantType].Item1(plant);//实时计算大招花费

                if (Board.Instance.theMoney < cost)//如果钱不够
                {
                    InGameText.Instance.ShowText($"大招需要{cost}金币", 5);//提示
                    return false;//直接返回
                }

                if (plant.SuperSkill())
                {
                    CustomCore.SuperSkills[plant.thePlantType].Item2(plant);//执行大招代码
                    plant.AnimSuperShoot();
                    __instance.UsedEvent(plant.thePlantColumn, plant.thePlantRow, cost);
                    __instance.OtherSuperSkill(plant);
                }

                return false;
            }

            return true;
        }

        [HarmonyPatch(nameof(Money.GetSuperSkillCost))]
        [HarmonyPrefix]
        public static bool PreGetSuperSkillCost(ref PlantType thePlantType, ref int __result)
        {
            if (CustomCore.SuperSkills.ContainsKey(thePlantType))
            {
                __result = CustomCore.SuperSkills[thePlantType].Item3;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Mouse))]
    public static class MousePatch
    {
        /// <summary>
        /// 防止手套拿起来大坚果
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("GetPlantsOnMouse")]
        public static void PostGetPlantsOnMouse(ref Il2CppSystem.Collections.Generic.List<Plant> __result)
        {
            for (int i = __result.Count - 1; i >= 0; i--)
            {
                if (__result.ToArray()[i] != null && TypeMgr.BigNut(__result.ToArray()[i].thePlantType))
                {
                    __result.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 处理自定义左键点击
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("LeftClickWithNothing")]
        public static void PostLeftClickWithNothing()
        {
            // 执行射线检测获取所有碰撞物体
            RaycastHit2D[] raycastHits = Physics2D.RaycastAll(
                Camera.main.ScreenToWorldPoint(Input.mousePosition),
                Vector2.zero
            );

            // 创建列表存储所有碰撞的游戏对象
            List<GameObject> hitGameObjects = [];
            foreach (RaycastHit2D raycastHit in raycastHits)
            {
                if (raycastHit.collider != null)
                {
                    hitGameObjects.Add(raycastHit.collider.gameObject);
                }
            }

            // 遍历所有碰撞的游戏对象
            foreach (GameObject gameObject in hitGameObjects)
            {
                //如果植物存在并且自定义单击列表中存在
                if (gameObject.TryGetComponent<Plant>(out var plant) &&
                    CustomCore.CustomPlantClicks.ContainsKey(plant.thePlantType))
                {
                    //触发单击
                    CustomCore.CustomPlantClicks[plant.thePlantType](plant);
                    return;
                }
            }
        }

        [HarmonyPatch(nameof(Mouse.Update))]
        [HarmonyPrefix]
        public static bool PreMouseClick(Mouse __instance)
        {
            if (!Input.GetMouseButtonDown(0))
                return true;
            if (__instance.theItemOnMouse == null)
                return true;
            var list = new List<Plant>();
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 rayPosition = new Vector2(worldPosition.x, worldPosition.y);

            // 从鼠标位置发射射线检测碰撞
            foreach (var hit in Physics2D.RaycastAll(rayPosition, Vector2.zero))
            {
                if (hit.collider == null || hit.collider.gameObject == null || hit.collider.gameObject.IsDestroyed())
                    continue;
                if (!hit.collider.gameObject.TryGetComponent<Plant>(out var plant))
                    continue;
                if (plant == null)
                    continue;
                list.Add(plant);
            }
            if (list.Count <= 0)
                return true;
            bool found = false;
            bool clear = false;
            var array = MixData.data.Cast<Il2CppSystem.Array>();
            List<Action<Plant>> executedActions = [];
            foreach (var item in list)
            {
                if (item == null)
                    continue;
                if (__instance.thePlantOnGlove != null && item == __instance.thePlantOnGlove)
                    continue;
                if (CustomCore.CustomClickCardOnPlantEvents.ContainsKey((item.thePlantType, __instance.thePlantTypeOnMouse)))
                {
                    bool block = false, clearOrigin = true;
                    foreach (var (action, onPlant) in CustomCore.CustomClickCardOnPlantEvents[(item.thePlantType, __instance.thePlantTypeOnMouse)])
                    {
                        if (executedActions.Contains(action)) // 判断，不然会多执行一次
                            continue;
                        if (onPlant.Trigger == CustomClickCardOnPlant.TriggerType.CardOnly && __instance.thePlantOnGlove != null)
                            continue;
                        if (onPlant.Trigger == CustomClickCardOnPlant.TriggerType.GloveOnly && __instance.thePlantOnGlove == null)
                            continue;
                        action(item);
                        executedActions.Add(action);
                        if (onPlant.BlockFusion)
                            block = true;
                        if (!onPlant.ClearOrigin)
                            clearOrigin = false;
                        found = true;
                    }
                    if (block)
                    {
                        return false;
                    }
                    if (clearOrigin)
                    {
                        clear = true;
                    }
                }
            }
            if (found && clear)
            {
                if (__instance.theCardOnMouse != null)
                {
                    __instance.theCardOnMouse.CD = 0f;
                    __instance.theCardOnMouse.isPickUp = false;
                    if (Board.Instance != null)
                    {
                        Board.Instance.UseSun(__instance.theCardOnMouse.theSeedCost);

                        // 高级旅行检查
                        if (Lawnf.TravelAdvanced(59))
                        {
                            Board.Instance.UseSun(Board.Instance.theSun / 2);
                        }
                    }
                }
                if (__instance.thePlantOnGlove != null)
                {
                    __instance.thePlantOnGlove.Die(Plant.DieReason.ByMix);
                    Glove glove = Glove.Instance;
                    if (glove != null)
                    {
                        float gloveCD = Lawnf.GetGloveCD();
                        glove.fullCD = gloveCD;
                        glove.CD = 0f;

                        // 特殊植物类型冷却时间调整
                        if (TypeMgr.IsPuff(__instance.thePlantTypeOnMouse) || TypeMgr.IsPot(__instance.thePlantTypeOnMouse) ||
                            TypeMgr.IsLily(__instance.thePlantTypeOnMouse) || TypeMgr.FlyingPlants(__instance.thePlantTypeOnMouse))
                        {
                            glove.CD = (glove.fullCD + glove.fullCD) / 3f;
                        }
                    }
                }
                Destroy(__instance.theItemOnMouse);
                __instance.ClearItemOnMouse(false);
            }
            if (!clear)
                return true;
            return !found;
        }
    }

    /// <summary>
    /// 处理对植物使用物品事件
    /// </summary>
    [HarmonyPatch(typeof(Plant))]
    public static class PlantPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("UseItem")]
        public static void PostUseItem(Plant __instance, ref BucketType type, ref Bucket bucket)
        {
            if (CustomCore.CustomUseItems.ContainsKey((__instance.thePlantType, type)))
            {
                CustomCore.CustomUseItems[(__instance.thePlantType, type)](__instance);
                UnityEngine.Object.Destroy(bucket.gameObject);
            }
        }

        [HarmonyPatch(nameof(Plant.Start))]
        [HarmonyPostfix]
        public static void PostStart(Plant __instance)
        {
            if (__instance != null && CustomCore.CustomOnMixEvent.ContainsKey((__instance.firstParent, __instance.secondParent)))
            {
                foreach (var action in CustomCore.CustomOnMixEvent[(__instance.firstParent, __instance.secondParent)])
                    action.Invoke(__instance);
            }
        }
    }

    /// <summary>
    /// 刷新卡牌贴图
    /// </summary>
    [HarmonyPatch(typeof(SeedLibrary))]
    public static class SeedLibraryPatch
    {
        /*[HarmonyPatch(nameof(SeedLibrary.Awake))]
        [HarmonyPostfix]
        public static void PostAwake(SeedLibrary __instance)
        {
            //为什么PostShowNormalCard会无限递归？？？
            //Grid
            __instance.transform.FindCardUIAndChangeSprite();
        }*/

        [HarmonyPatch(nameof(SeedLibrary.Start))]
        [HarmonyPostfix]
        public static void PostStart(SeedLibrary __instance)
        {
            // 注册自定义卡牌
            GameObject? MyColorfulCard = Utils.GetColorfulCardGameObject();
            Dictionary<PlantType, List<Transform?>> parents_colorful = new Dictionary<PlantType, List<Transform?>>();
            List<PlantType> cardsOnSeedBank = new List<PlantType>();
            Dictionary<PlantType, List<bool>> cardsOnSeedBankExtra = new Dictionary<PlantType, List<bool>>();
            GameObject? seedGroup = null;
            if (Board.Instance != null && !Board.Instance.isIZ)
                seedGroup = InGameUI.Instance.SeedBank.transform.GetChild(0).gameObject;
            else if (Board.Instance != null && Board.Instance.isIZ)
                seedGroup = InGameUI_IZ.Instance.transform.FindChild("SeedBank/SeedGroup").gameObject;
            if (seedGroup == null)
                return;
            for (int i = 0; i < seedGroup.transform.childCount; i++)
            {
                GameObject seed = seedGroup.transform.GetChild(i).gameObject;
                if (seed.transform.childCount > 0)
                {
                    cardsOnSeedBank.Add(seed.transform.GetChild(0).GetComponent<CardUI>().thePlantType);
                    if (!cardsOnSeedBankExtra.ContainsKey(seed.transform.GetChild(0).GetComponent<CardUI>().thePlantType))
                        cardsOnSeedBankExtra.Add(seed.transform.GetChild(0).GetComponent<CardUI>().thePlantType, new List<bool>() { seed.transform.GetChild(0).GetComponent<CardUI>().isExtra });
                    else
                        cardsOnSeedBankExtra[seed.transform.GetChild(0).GetComponent<CardUI>().thePlantType].Add(seed.transform.GetChild(0).GetComponent<CardUI>().isExtra);
                }
            }
            if (MyColorfulCard == null)
                return;
            foreach (var card in CustomCore.CustomCards)
            {
                foreach (Func<Transform?> cardFunc in card.Value)
                {
                    Transform? result = cardFunc();
                    if (!(parents_colorful.ContainsKey(card.Key) && parents_colorful[card.Key].Contains(result)))
                    {
                        GameObject TempCard = Instantiate(MyColorfulCard, result);
                        if (TempCard != null)
                        {
                            //设置父节点
                            //激活
                            TempCard.SetActive(true);
                            //设置位置
                            TempCard.transform.position = MyColorfulCard.transform.position;
                            TempCard.transform.localPosition = MyColorfulCard.transform.localPosition;
                            TempCard.transform.localScale = MyColorfulCard.transform.localScale;
                            TempCard.transform.localRotation = MyColorfulCard.transform.localRotation;
                            //背景图片
                            // 设置背景植物图标
                            Image image = TempCard.transform.GetChild(0).GetChild(0).GetComponent<Image>();
                            image.sprite = GameAPP.resourcesManager.plantPreviews[card.Key].GetComponent<SpriteRenderer>().sprite;
                            image.SetNativeSize();
                            // 设置背景价格
                            TempCard.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = PlantDataLoader.plantDatas[card.Key].field_Public_Int32_1.ToString();
                            //卡片
                            CardUI component = TempCard.transform.GetChild(1).GetComponent<CardUI>();
                            component.gameObject.SetActive(true);
                            //修改图片
                            Mouse.Instance.ChangeCardSprite(card.Key, component);
                            // 修改缩放
                            TempCard.transform.GetChild(1).GetComponent<BoxCollider2D>().enabled = true;
                            RectTransform bgRect = TempCard.transform.GetChild(0).GetChild(0).GetComponent<RectTransform>();
                            RectTransform packetRect = TempCard.transform.GetChild(1).GetChild(0).GetComponent<RectTransform>();
                            bgRect.localScale = packetRect.localScale;
                            bgRect.sizeDelta = packetRect.sizeDelta;
                            //设置数据
                            component.thePlantType = card.Key;
                            component.theSeedType = (int)card.Key;
                            component.theSeedCost = PlantDataLoader.plantDatas[card.Key].field_Public_Int32_1;
                            component.fullCD = PlantDataLoader.plantDatas[card.Key].field_Public_Single_2;
                            if (cardsOnSeedBank.Contains(card.Key))
                                TempCard.transform.GetChild(1).gameObject.SetActive(false);
                            CheckCardState customComponent = TempCard.AddComponent<CheckCardState>();
                            customComponent.card = TempCard;
                            customComponent.cardType = component.thePlantType;
                            if (!parents_colorful.ContainsKey(card.Key))
                                parents_colorful.Add(card.Key, new List<Transform?>() { result });
                            else
                                parents_colorful[card.Key].Add(result);
                        }
                    }
                }
            }

            GameObject? MyNormalCard = Utils.GetNormalCardGameObject();
            Dictionary<PlantType, List<Transform?>> parents_normal = new Dictionary<PlantType, List<Transform?>>();
            if (MyNormalCard == null)
                return;
            foreach (var card in CustomCore.CustomNormalCards)
            {
                foreach (Func<Transform?> cardFunc in card.Value)
                {
                    Transform? result = cardFunc();
                    if (!(parents_normal.ContainsKey(card.Key) && parents_normal[card.Key].Contains(result)))
                    {
                        GameObject TempCard = Instantiate(MyNormalCard, result);
                        if (TempCard != null)
                        {
                            //设置父节点
                            //激活
                            TempCard.SetActive(true);
                            //设置位置
                            TempCard.transform.position = MyNormalCard.transform.position;
                            TempCard.transform.localPosition = MyNormalCard.transform.localPosition;
                            TempCard.transform.localScale = MyNormalCard.transform.localScale;
                            TempCard.transform.localRotation = MyNormalCard.transform.localRotation;
                            //背景图片
                            // 设置背景植物图标
                            Image image = TempCard.transform.GetChild(0).GetChild(0).GetComponent<Image>();
                            image.sprite = GameAPP.resourcesManager.plantPreviews[card.Key].GetComponent<SpriteRenderer>().sprite;
                            image.SetNativeSize();
                            // 设置背景价格
                            TempCard.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = PlantDataLoader.plantDatas[card.Key].field_Public_Int32_1.ToString();
                            //卡片
                            CardUI component = TempCard.transform.GetChild(2).GetComponent<CardUI>(); // 主卡
                            component.gameObject.SetActive(true);
                            CardUI component1 = TempCard.transform.GetChild(1).GetComponent<CardUI>(); // 副卡
                            component1.gameObject.SetActive(true);
                            //修改图片
                            Mouse.Instance.ChangeCardSprite(card.Key, component);
                            Mouse.Instance.ChangeCardSprite(card.Key, component1);
                            // 修改缩放
                            TempCard.transform.GetChild(2).GetComponent<BoxCollider2D>().enabled = true;
                            TempCard.transform.GetChild(1).GetComponent<BoxCollider2D>().enabled = true;
                            RectTransform bgRect = TempCard.transform.GetChild(0).GetChild(0).GetComponent<RectTransform>();
                            RectTransform packetRect = TempCard.transform.GetChild(2).GetChild(0).GetComponent<RectTransform>();
                            bgRect.localScale = packetRect.localScale;
                            bgRect.sizeDelta = packetRect.sizeDelta;
                            //设置数据
                            component.thePlantType = card.Key;
                            component.theSeedType = (int)card.Key;
                            component.theSeedCost = PlantDataLoader.plantDatas[card.Key].field_Public_Int32_1;
                            component.fullCD = PlantDataLoader.plantDatas[card.Key].field_Public_Single_2;
                            //设置副卡数据
                            component1.thePlantType = card.Key;
                            component1.theSeedType = (int)card.Key;
                            component1.theSeedCost = PlantDataLoader.plantDatas[card.Key].field_Public_Int32_1 * 2;
                            component1.fullCD = PlantDataLoader.plantDatas[card.Key].field_Public_Single_2;
                            if (cardsOnSeedBankExtra.ContainsKey(card.Key) && cardsOnSeedBankExtra[card.Key].Contains(true))
                                TempCard.transform.GetChild(1).gameObject.SetActive(false);
                            if (cardsOnSeedBankExtra.ContainsKey(card.Key) && cardsOnSeedBankExtra[card.Key].Contains(false))
                                TempCard.transform.GetChild(2).gameObject.SetActive(false);
                            CheckCardState customComponent = TempCard.AddComponent<CheckCardState>();
                            customComponent.card = TempCard;
                            customComponent.cardType = component.thePlantType;
                            customComponent.isNormalCard = true;
                            if (!parents_normal.ContainsKey(card.Key))
                                parents_normal.Add(card.Key, new List<Transform?>() { result });
                            else
                                parents_normal[card.Key].Add(result);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 进入一局游戏，显示二创植物Button
    /// </summary>
    [HarmonyPatch(typeof(Board))]
    public static class Board_Patch
    {
        [HarmonyPatch(nameof(Board.Start))]
        [HarmonyPostfix]
        public static void PostStart()
        {
            SelectCustomPlants.InitCustomCards();
            if (TravelMgr.Instance == null)
                return;
            if (TravelMgr.Instance.GetData("LoadByEndless") is null)
                TravelMgr.Instance.SetData("LoadByEndless", false);
            if ((TravelMgr.Instance.GetData("CustomBuffsLevel") is null ||
                TravelMgr.Instance.GetData<int[]>("CustomBuffsLevel").SequenceEqual(new int[CustomCore.CustomAdvancedBuffs.Count])) &&
                !TravelMgr.Instance.GetData<bool>("LoadByEndless"))
            {
                TravelMgr.Instance.SetData("CustomBuffsLevel", new int[CustomCore.CustomAdvancedBuffs.Count]);
            }
        }

        [HarmonyPatch(nameof(Board.Update))]
        [HarmonyPostfix]
        public static void PostUpdate()
        {
            if (TravelMgr.Instance == null)
                return;
            try
            {
                var array = (int[])TravelMgr.Instance.GetData("CustomBuffsLevel");
                if (array is null)
                    return;

                foreach (var (key, value) in CustomCore.CustomBuffsLevel)
                {
                    var result = Utils.IsMultiLevelBuff(key.Item1, key.Item2);
                    if (!result.Item1)
                        continue;
                    int index = result.Item2;
                    if (index >= array.Length)
                        continue;
                    switch (key.Item1)
                    {
                        case BuffType.AdvancedBuff:
                            {
                                if (index >= TravelMgr.Instance.advancedUpgrades.Count)
                                    break;
                                if (!TravelMgr.Instance.advancedUpgrades[key.Item2])
                                    array[index] = 0;
                                if (array[index] <= 0 && TravelMgr.Instance.advancedUpgrades[key.Item2])
                                    array[index] = 1;
                            }
                            break;
                        case BuffType.UltimateBuff:
                            {
                                if (index >= TravelMgr.Instance.ultimateUpgrades.Count)
                                    break;
                                if (TravelMgr.Instance.ultimateUpgrades[key.Item2] <= 0)
                                    array[index] = 0;
                                if (array[index] <= 0 && TravelMgr.Instance.ultimateUpgrades[key.Item2] >= 1)
                                    array[index] = TravelMgr.Instance.ultimateUpgrades[key.Item2];
                            }
                            break;
                        case BuffType.Debuff:
                            {
                                if (index >= TravelMgr.Instance.debuff.Count)
                                    break;
                                if (!TravelMgr.Instance.debuff[key.Item2])
                                    array[index] = 0;
                                if (array[index] <= 0 && TravelMgr.Instance.debuff[key.Item2])
                                    array[index] = 1;
                            }
                            break;
                        case BuffType.UnlockPlant:
                            {
                                if (index >= TravelMgr.Instance.unlockPlant.Count)
                                    break;
                                if (!TravelMgr.Instance.unlockPlant[key.Item2])
                                    array[index] = 0;
                                if (array[index] <= 0 && TravelMgr.Instance.unlockPlant[key.Item2])
                                    array[index] = 1;
                            }
                            break;
                    }
                }
            }
            catch (ArgumentException) { }
        }

        [HarmonyPatch(nameof(Board.WheatLimit))]
        [HarmonyPrefix]
        public static bool PreWheatLimit(ref PlantType plantType, ref bool __result)
        {
            if (CustomCore.CustomUltimatePlants.Contains(plantType))
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
#if DEBUG_FEATURE__ENABLE_MULTI_LEVEL_BUFF
    #region 多级词条同步
    [HarmonyPatch(typeof(RandomZombie))]
    public static class RandomZombie_Patch
    {
        [HarmonyPatch(nameof(RandomZombie.FirstArmorFall))]
        [HarmonyPostfix]
        public static void Postfix()
        {
            // 普通词条对应升级
            for (int i = (int)CustomCore.variables[0]; i < TravelMgr.advancedBuffs.Count; i++)
            {
                var result = Utils.IsMultiLevelBuff(BuffType.AdvancedBuff, i);
                var index = CustomCore.CustomBuffsLevel.Where(kvp => kvp.Key.Item1 == BuffType.AdvancedBuff && kvp.Key.Item3 == i).Select(kvp => kvp.Key.Item2).ToList();
                foreach (var ii in index)
                    if (result.Item1 && TravelMgr.Instance.ultimateUpgrades[ii] == 0 && TravelMgr.Instance.advancedUpgrades[i])
                        foreach (var value in result.Item2)
                            TravelMgr.Instance.ultimateUpgrades[(int)CustomCore.variables[0] + value.Item2] = 1;
            }
            // Debuff词条对应升级
            for (int i = (int)CustomCore.variables[0]; i < TravelMgr.debuffs.Count; i++)
            {
                var result = Utils.IsMultiLevelBuff(BuffType.Debuff, i);
                var index = CustomCore.CustomBuffsLevel.Where(kvp => kvp.Key.Item1 == BuffType.Debuff && kvp.Key.Item3 == i).Select(kvp => kvp.Key.Item2).ToList();
                foreach (var ii in index)
                    if (result.Item1 && TravelMgr.Instance.ultimateUpgrades[i] == 0 && TravelMgr.Instance.debuff[i])
                        foreach (var value in result.Item2)
                            TravelMgr.Instance.ultimateUpgrades[(int)CustomCore.variables[0] + value.Item2] = 1;
            }
        }
    }
    #endregion
#endif

    /// <summary>
    /// 强究词条修改图标
    /// </summary>
    [HarmonyPatch(typeof(TravelBuff))]
    public static class TravelBuffPatch
    {
        [HarmonyPatch(nameof(TravelBuff.ChangeSprite))]
        [HarmonyPrefix]
        public static void Prefix(TravelBuff __instance)
        {
            var list = CustomCore.CustomUltimateBuffs.
                    Where(kvp => kvp.Key == __instance.theBuffNumber).
                    Select(kvp => kvp.Value).
                    ToList();
            if (__instance.theBuffType == (int)BuffType.UltimateBuff && list.Count > 0)
            {
                foreach (var item in list)
                {
                    if (item.Item1 == PlantType.Nothing)
                        __instance.thePlantType = PlantType.EndoFlame;
                    else
                        __instance.thePlantType = item.Item1;
                }
            }

            if (__instance.theBuffType == (int)BuffType.AdvancedBuff && CustomCore.CustomAdvancedBuffs.ContainsKey(__instance.theBuffNumber))
            {
                __instance.thePlantType = CustomCore.CustomAdvancedBuffs[__instance.theBuffNumber].Item1;
            }
        }
    }

    /// <summary>
    /// 二创词条文本染色
    /// </summary>
    [HarmonyPatch(typeof(TravelBuffOptionButton))]
    public static class TravelBuffOptionButtonPatch
    {
        [HarmonyPatch(nameof(TravelBuffOptionButton.SetBuff))]
        public static void PostSetBuff(TravelBuffOptionButton __instance, ref BuffType buffType, ref int buffIndex)
        {
            if (buffType is BuffType.AdvancedBuff && CustomCore.CustomAdvancedBuffs.ContainsKey(buffIndex)
                && CustomCore.CustomAdvancedBuffs[buffIndex].Item5 is not null)
            {
                __instance.introduce.text = $"<color={CustomCore.CustomAdvancedBuffs[buffIndex].Item5}>{__instance.introduce.text}</color>";
            }
        }

        /// <summary>
        /// 强究词条显示植物修复
        /// </summary>
        [HarmonyPatch(nameof(TravelBuffOptionButton.SetPlant), new Type[] { })]
        [HarmonyPrefix]
        public static bool PreSetPlant(TravelBuffOptionButton __instance)
        {
            var list = CustomCore.CustomUltimateBuffs.
                Where(kvp => kvp.Key == __instance.buffIndex).
                ToList();
            if (__instance.buffType == BuffType.UltimateBuff && list.Count > 0)
            {
                foreach (var value in list)
                {
                    if (value.Value.Item1 == PlantType.Nothing)
                        __instance.SetPlant(PlantType.EndoFlame);
                    else
                        __instance.SetPlant(value.Value.Item1);
                }
                return false;
            }
            return true;
        }
    }

#if DEBUG_FEATURE__ENABLE_MULTI_LEVEL_BUFF
    #region 多级词条修复数组
    [HarmonyPatch(typeof(TravelLookMenu))]
    public static class TravelLookMenuPatch
    {
        /// <summary>
        /// 修复数组，不然会多一个
        /// </summary>
        [HarmonyPatch(nameof(TravelLookMenu.GetUltiBuffs))]
        [HarmonyPostfix]
        public static void PostGetUltiBuffs(TravelLookMenu __instance, ref Il2CppSystem.Collections.Generic.List<Vector2Int> __result)
        {
            Il2CppSystem.Collections.Generic.List<Vector2Int> result = new Il2CppSystem.Collections.Generic.List<Vector2Int>();

            // 遍历升级数组
            for (int i = 0; i < __instance.manager.ultimateUpgrades.Length; i++)
            {
                if (CustomCore.CustomBuffsLevel.Count > 0 && CustomCore.CustomBuffsLevel.Any(kvp => ((int)CustomCore.variables[0] + kvp.Key.Item2) == i && kvp.Key.Item1 != BuffType.UltimateBuff))
                    continue;
                if (CustomCore.CustomBuffsLevel.Count > 0 && CustomCore.CustomBuffsLevel.Any(kvp => kvp.Key.Item1 == BuffType.UltimateBuff) && i == __instance.manager.ultimateUpgrades.Length - 1)
                    break;
                // 检查是否已解锁或显示所有
                if (__instance.manager.ultimateUpgrades[i] != 0 || __instance.showAll)
                {
                    // 添加索引和等级到结果列表
                    result.Add(new Vector2Int(i, __instance.manager.ultimateUpgrades[i]));
                }
            }
            __result = result;
        }
    }
    #endregion
#endif

    /// <summary>
    /// 二创词条文本染色
    /// </summary>
    [HarmonyPatch(typeof(TravelLookBuff))]
    public static class TravelLookBuffPatch
    {
        [HarmonyPatch(nameof(TravelLookBuff.SetBuff))]
        [HarmonyPostfix]
        public static void PostSetBuff(TravelLookBuff __instance, ref BuffType buffType, ref int buffIndex)
        {
            if (buffType is BuffType.AdvancedBuff && CustomCore.CustomAdvancedBuffs.ContainsKey(buffIndex)
                && CustomCore.CustomAdvancedBuffs[buffIndex].Item5 is not null)
            {
                __instance.introduce.text = $"<color={CustomCore.CustomAdvancedBuffs[buffIndex].Item5}>{__instance.introduce.text}</color>";
            }

            if (CustomCore.CustomBuffsBg.ContainsKey((buffType, buffIndex)))
                __instance.SetBackground(CustomCore.CustomBuffsBg[(buffType, buffIndex)]);

            var result = Utils.IsMultiLevelBuff(__instance.buffType, __instance.buffIndex);
            try
            {
                if (result.Item1)
                {
                    var array = (int[])TravelMgr.Instance.GetData("CustomBuffsLevel");
                    if (array is null)
                        return;
                    int index = result.Item2;
                    var list = CustomCore.CustomBuffsLevel.Where(kvp => kvp.Key.Item1 == __instance.buffType && kvp.Key.Item2 == __instance.buffIndex).ToList();
                    int maxLevel = 1;
                    if (list.Count > 0)
                        maxLevel = list[0].Value.Item2;
                    if (TravelLookMenu.Instance.showAll)
                    {
                        __instance.SetText(array[index] != 0, array[index]);
                        if (array[index] <= maxLevel &&
                            array[index] != 0)
                        {
                            if (maxLevel > 1)
                                __instance.SetText($"已开启（{array[index]}级）");
                            else
                                __instance.SetText($"已开启");
                        }
                        return;
                    }
                    else
                    {
                        if (array[index] < maxLevel && maxLevel != 1)
                        {
                            if (array[index] >= maxLevel)
                                __instance.SetText("已满级");
                            else
                                __instance.SetText($"{array[index]}级");
                        }
                        if (array[index] >= maxLevel)
                        {
                            __instance.SetText("已满级");
                        }
                        TravelMgr.Instance.SetData("CustomBuffsLevel", array);
                    }
                }
            }
            catch (ArgumentException)
            {
                MelonLogger.Msg("Can't get data");
            }
#if DEBUG_FEATURE__ENABLE_MULTI_LEVEL_BUFF
            #region 多级词条显示修复
            // 多级词条显示修复
            if (__instance == null)
                return;
            var result = Utils.IsMultiLevelBuff(__instance.buffType, __instance.buffIndex);
            if (result.Item1)
            {
                foreach (var value in result.Item2)
                {
                    int index = (int)CustomCore.variables[0] + value.Item2;
                    Il2CppStructArray<int> upgrades = __instance.manager.ultimateUpgrades;
                    if (TravelLookMenu.Instance.showAll)
                    {
                        __instance.SetText(upgrades[index] != 0, upgrades[index]);
                        if (upgrades[index] <= CustomCore.CustomBuffsLevel[value] &&
                            upgrades[index] != 0)
                        {
                            if (CustomCore.CustomBuffsLevel[value] > 1)
                                __instance.SetText($"已开启（{upgrades[index]}级）");
                            else
                                __instance.SetText($"已开启");
                        }
                        else
                        {
                            __instance.SetText("已关闭");
                        }
                    }
                    else
                    {
                        if (upgrades[index] < CustomCore.CustomBuffsLevel[value] && CustomCore.CustomBuffsLevel[value] != 1)
                        {
                            if (upgrades[index] >= CustomCore.CustomBuffsLevel[value])
                            {
                                __instance.SetText("已满级");
                            }
                            else
                                __instance.SetText($"{upgrades[index]}级");
                        }
                        if (upgrades[index] >= CustomCore.CustomBuffsLevel[value])
                        {
                            __instance.SetText("已满级");
                        }
                    }
                }
            }
            #endregion
#endif
        }
        /// <summary>
        /// 高级词条升级处理
        /// </summary>
        [HarmonyPatch(nameof(TravelLookBuff.OnMouseUpAsButton))]
        [HarmonyPrefix]
        public static bool PreOnMouseUpAsButton(TravelLookBuff __instance)
        {
            var result = Utils.IsMultiLevelBuff(__instance.buffType, __instance.buffIndex);
            bool reset = false;
            if (result.Item1)
            {
                try
                {
                    var array = (int[])TravelMgr.Instance.GetData("CustomBuffsLevel");
                    if (array is null)
                        return true;
                    int index = result.Item2;
                    var list = CustomCore.CustomBuffsLevel.Where(kvp => kvp.Key.Item1 == __instance.buffType && kvp.Key.Item2 == __instance.buffIndex).ToList();
                    int maxLevel = 1;
                    if (list.Count > 0)
                        maxLevel = list[0].Value.Item2;
                    if (TravelLookMenu.Instance.showAll)
                    {
                        array[index] = array[index] + 1;
                        if (array[index] > maxLevel)
                        {
                            array[index] = 0;
                        }
                        if (array[index] == 0)
                        {
                            switch (__instance.buffType)
                            {
                                case BuffType.AdvancedBuff:
                                    TravelMgr.Instance.advancedUpgrades[__instance.buffIndex] = false;
                                    break;
                                case BuffType.UltimateBuff:
                                    TravelMgr.Instance.ultimateUpgrades[__instance.buffIndex] = 0;
                                    break;
                                case BuffType.Debuff:
                                    TravelMgr.Instance.debuff[__instance.buffIndex] = false;
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            switch (__instance.buffType)
                            {
                                case BuffType.AdvancedBuff:
                                    TravelMgr.Instance.advancedUpgrades[__instance.buffIndex] = true;
                                    break;
                                case BuffType.UltimateBuff:
                                    TravelMgr.Instance.ultimateUpgrades[__instance.buffIndex] = array[index];
                                    break;
                                case BuffType.Debuff:
                                    TravelMgr.Instance.debuff[__instance.buffIndex] = true;
                                    break;
                                default:
                                    break;
                            }
                        }
                        __instance.SetText(array[index] != 0, array[index]);
                        if (array[index] <= maxLevel &&
                            array[index] != 0)
                        {
                            if (maxLevel > 1)
                                __instance.SetText($"已开启（{array[index]}级）");
                            else
                                __instance.SetText($"已开启");
                        }
                        TravelMgr.Instance.SetData("CustomBuffsLevel", array);
                        return false;
                    }
                    else
                    {
                        if (array[index] < maxLevel && Lawnf.TravelAdvanced(54) && maxLevel != 1)
                        {
                            array[index] = array[index] + 1;
                            reset = true;
                            if (array[index] >= maxLevel)
                                __instance.SetText("已满级");
                            else
                                __instance.SetText($"{array[index]}级");
                        }
                        if (array[index] >= maxLevel)
                        {
                            __instance.SetText("已满级");
                        }
                        TravelMgr.Instance.SetData("CustomBuffsLevel", array);
                    }
                }
                catch (ArgumentException)
                {
                    MelonLogger.Msg("Can't get data");
                }
            }
            if (reset)
            {
                __instance.manager.advancedUpgrades[54] = false;
                return false;
            }
            return true;
        }
#if DEBUG_FEATURE__ENABLE_MULTI_LEVEL_BUFF
        #region 多级词条升级
        /// <summary>
        /// 高级词条升级处理
        /// </summary>
        [HarmonyPatch(nameof(TravelLookBuff.OnMouseUpAsButton))]
        [HarmonyPrefix]
        public static bool PreOnMouseUpAsButton(TravelLookBuff __instance)
        {
            var result = Utils.IsMultiLevelBuff(__instance.buffType, __instance.buffIndex);
            bool reset = false;
            if (result.Item1)
            {
                foreach (var value in result.Item2)
                {
                    int index = (int)CustomCore.variables[0] + value.Item2;
                    if (TravelLookMenu.Instance.showAll)
                    {
                        Il2CppStructArray<int> upgrades = __instance.manager.ultimateUpgrades;
                        upgrades[index] = upgrades[index] + 1;
                        if (upgrades[index] > CustomCore.CustomBuffsLevel[value])
                            upgrades[index] = 0;
                        __instance.SetText(upgrades[index] != 0, upgrades[index]);
                        if (upgrades[index] <= CustomCore.CustomBuffsLevel[value] &&
                            upgrades[index] != 0)
                        {
                            if (CustomCore.CustomBuffsLevel[value] > 1)
                                __instance.SetText($"已开启（{upgrades[index]}级）");
                            else
                                __instance.SetText($"已开启");
                        }
                        return false;
                    }
                    else
                    {
                        Il2CppStructArray<int> upgrades = __instance.manager.ultimateUpgrades;
                        if (upgrades[index] < CustomCore.CustomBuffsLevel[value] && Lawnf.TravelAdvanced(54) && CustomCore.CustomBuffsLevel[value] != 1)
                        {
                            upgrades[index] = upgrades[index] + 1;
                            reset = true;
                            if (upgrades[index] >= CustomCore.CustomBuffsLevel[value])
                                __instance.SetText("已满级");
                            else
                                __instance.SetText($"{upgrades[index]}级");
                        }
                        if (upgrades[index] >= CustomCore.CustomBuffsLevel[value])
                        {
                            __instance.SetText("已满级");
                        }
                    }
                }
            }
            if (reset)
            {
                __instance.manager.advancedUpgrades[54] = false;
                return false;
            }
            return true;
        }
        #endregion
#endif
    }

    /// <summary>
    /// 注册二创词条
    /// </summary>
    [HarmonyPatch(typeof(TravelMgr))]
    public static class TravelMgrPatch
    {
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void PostAwake(TravelMgr __instance)
        {
            if (CustomCore.CustomAdvancedBuffs.Count > 0)//普通词条
            {
                bool[] newAdv = new bool[__instance.advancedUpgrades.Count + CustomCore.CustomAdvancedBuffs.Count];
                Array.Copy(__instance.advancedUpgrades, newAdv, __instance.advancedUpgrades.Length);
                __instance.advancedUpgrades = newAdv;
            }

            if (CustomCore.CustomUltimateBuffs.Count > 0)//强究词条
            {
                int[] newUlti = new int[__instance.ultimateUpgrades.Count + CustomCore.CustomUltimateBuffs.Count];
                Array.Copy(__instance.ultimateUpgrades, newUlti, __instance.ultimateUpgrades.Length);
                __instance.ultimateUpgrades = newUlti;
            }

            if (CustomCore.CustomDebuffs.Count > 0)//僵尸词条
            {
                bool[] newdeb = new bool[__instance.debuff.Count + CustomCore.CustomDebuffs.Count];
                Array.Copy(__instance.debuff, newdeb, __instance.debuff.Length);
                __instance.debuff = newdeb;
            }

            if (CustomCore.CustomUnlockBuffs.Count > 0)
            {
                bool[] newUnlock = new bool[__instance.unlockPlant.Count + CustomCore.CustomUnlockBuffs.Count];
                Array.Copy(__instance.unlockPlant, newUnlock, __instance.unlockPlant.Length);
                __instance.unlockPlant = newUnlock;
            }

#if DEBUG_FEATURE__ENABLE_MULTI_LEVEL_BUFF
            #region 多级词条扩容
            if (CustomCore.CustomBuffsLevel.Count > 0)//高级词条
            {
                CustomCore.variables[0] = __instance.ultimateUpgrades.Length;
                int length = CustomCore.CustomBuffsLevel.Count(kvp => kvp.Value != 1);
                int[] newLevel = new int[__instance.ultimateUpgrades.Length + length];
                Array.Copy(__instance.ultimateUpgrades, newLevel, __instance.ultimateUpgrades.Length);
                __instance.ultimateUpgrades = newLevel;
            }
            #endregion
#endif

            //foreach (PlantType plantType in CustomCore.CustomUltimatePlants) // 注册强究植物
            //{
            //    if (!TravelMgr.allStrongUltimtePlant.Contains(plantType))
            //        TravelMgr.allStrongUltimtePlant.Add(plantType);
            //}
        }

        [HarmonyPatch(nameof(TravelMgr.Start))]
        [HarmonyPostfix]
        public static void PostStart(TravelMgr __instance)
        {
            if (__instance.GetData("CustomBuffsLevel") is null)
            {
                __instance.SetData("CustomBuffsLevel", new int[CustomCore.CustomBuffsLevel.Count]);
            }
            if (__instance.GetData("LoadByEndless") is null)
                __instance.SetData("LoadByEndless", false);
            if (!__instance.GetData<bool>("LoadByEndless"))
            {
                __instance.SetData("CustomBuffsLevel", new int[CustomCore.CustomBuffsLevel.Count]);
            }
            TravelMgr.Instance.SetData("LoadByEndless", false); // 重置标志位，避免进入其他模式后不重置
        }

        /// <summary>
        /// 按自定义条件筛词条
        /// </summary>
        [HarmonyPatch("GetAdvancedBuffPool")]
        [HarmonyPostfix]
        public static void PostGetAdvancedBuffPool(ref Il2CppSystem.Collections.Generic.List<int> __result)
        {
            for (int i = __result.Count - 1; i >= 0; i--)
            {
                if (CustomCore.CustomAdvancedBuffs.ContainsKey(__result.ToArray()[i]) &&
                    !CustomCore.CustomAdvancedBuffs[__result.ToArray()[i]].Item3())
                {
                    __result.Remove(__result.ToArray()[i]);
                }
            }
        }

        [HarmonyPatch(nameof(TravelMgr.GetAdvancedText))]
        [HarmonyPostfix]
        public static void PostGetAdvancedText(ref int index, ref string __result)
        {
            if (CustomCore.CustomAdvancedBuffs.ContainsKey(index) && CustomCore.CustomAdvancedBuffs[index].Item5 is not null)
            {
                __result = $"<color={CustomCore.CustomAdvancedBuffs[index].Item5}>{__result}</color>";
            }
        }

        [HarmonyPatch(nameof(TravelMgr.GetPlantTypeByAdvBuff))]
        [HarmonyPostfix]
        public static void PostGetPlantTypeByAdvBuff(ref int index, ref PlantType __result)
        {
            if (CustomCore.CustomAdvancedBuffs.ContainsKey(index) && CustomCore.CustomAdvancedBuffs[index].Item1 is not PlantType.Nothing)
            {
                __result = CustomCore.CustomAdvancedBuffs[index].Item1;
            }
        }

        [HarmonyPatch(nameof(TravelMgr.GetUltimateText))]
        [HarmonyPostfix]
        public static void PostGetUltimateText(ref int index, ref string __result)
        {
            if (CustomCore.CustomUltimateBuffs.ContainsKey(index))
                __result = CustomCore.CustomUltimateBuffs[index].Item2;
        }

#if DEBUG_FEATURE__ENABLE_MULTI_LEVEL_BUFF
        #region 多级词条同步
        [HarmonyPatch(nameof(TravelMgr.Start))]
        [HarmonyPostfix]
        public static void PostStart(TravelMgr __instance)
        {
            // 普通词条对应升级
            for (int i = (int)CustomCore.variables[0]; i < __instance.advancedUpgrades.Count; i++)
            {
                var result = Utils.IsMultiLevelBuff(BuffType.AdvancedBuff, i);
                foreach (var ii in result.Item2)
                    if (result.Item1 && __instance.ultimateUpgrades[(int)CustomCore.variables[0] + ii.Item2] == 0 && __instance.advancedUpgrades[i])
                        foreach (var value in result.Item2)
                            __instance.ultimateUpgrades[(int)CustomCore.variables[0] + value.Item2] = 1;
            }
            // Debuff词条对应升级
            for (int i = (int)CustomCore.variables[0]; i < __instance.debuff.Count; i++)
            {
                var result = Utils.IsMultiLevelBuff(BuffType.Debuff, i);
                foreach (var ii in result.Item2)
                    if (result.Item1 && __instance.ultimateUpgrades[(int)CustomCore.variables[0] + ii.Item2] == 0 && __instance.debuff[i])
                        foreach (var value in result.Item2)
                            __instance.ultimateUpgrades[(int)CustomCore.variables[0] + value.Item2] = 1;
            }
        }
        #endregion
#endif
    }

    [HarmonyPatch(typeof(TravelBuffMenu))]
    public static class TravelBuffMenuPatch
    {
        [HarmonyPatch(nameof(TravelBuffMenu.RefeshOptions))]
        [HarmonyPostfix]
        public static void PostRefreshOptions(TravelBuffMenu __instance, ref bool refresh)
        {
            int start = -1, end = -1;
            for (int i = 0; i < __instance.options.Count; i++)
            {
                
            }
            if (!__instance.isDebuff)
            {

            }
        }
    }

    /// <summary>
    /// 词条商店卡片贴图(是否有效没试未知)
    /// </summary>
    [HarmonyPatch(typeof(TravelStore))]
    public static class TravelStorePatch
    {
        [HarmonyPatch("RefreshBuff")]
        [HarmonyPostfix]
        public static void PostRefreshBuff(TravelStore __instance)
        {
            foreach (var travelBuff in __instance.gameObject.GetComponentsInChildren<TravelBuff>())
            {
                if (travelBuff.theBuffType is (int)BuffType.AdvancedBuff &&
                    CustomCore.CustomAdvancedBuffs.ContainsKey(travelBuff.theBuffNumber))
                {
                    travelBuff.cost = CustomCore.CustomAdvancedBuffs[travelBuff.theBuffNumber].Item4;
                    travelBuff.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text =
                        $"￥{CustomCore.CustomAdvancedBuffs[travelBuff.theBuffNumber].Item4}";
                }

                if (travelBuff.theBuffType is (int)BuffType.UltimateBuff &&
                    CustomCore.CustomUltimateBuffs.ContainsKey(travelBuff.theBuffNumber))
                {
                    travelBuff.cost = CustomCore.CustomUltimateBuffs[travelBuff.theBuffNumber].Item3;
                    travelBuff.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text =
                        $"￥{CustomCore.CustomUltimateBuffs[travelBuff.theBuffNumber].Item3.ToString()}";
                }
            }
        }
    }

    /// <summary>
    /// 自定义类型，特性附加
    /// </summary>
    [HarmonyPatch(typeof(TypeMgr))]
    public static class TypeMgrPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("BigNut")]
        public static bool PreBigNut(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.BigNut.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.BigNut.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("BigZombie")]
        public static bool PreBigZombie(ref ZombieType theZombieType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.BigZombie.Contains(theZombieType))
            {
                __result = true;
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(TypeMgr.IsDriverZombie))]
        public static bool PreDriverZombie(ref ZombieType theZombieType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.DriverZombie.Contains(theZombieType))
            {
                __result = true;
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("DoubleBoxPlants")]
        public static bool PreDoubleBoxPlants(ref PlantType thePlantType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.DoubleBoxPlants.Contains(thePlantType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.DoubleBoxPlants.TryGetValue(thePlantType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        /*[HarmonyPrefix]
        [HarmonyPatch(nameof(TypeMgr.EliteZombie))]
        public static bool PreEliteZombie(ref ZombieType theZombieType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.EliteZombie.Contains(theZombieType))
            {
                __result = true;
                return false;
            }

            return true;
        }*/

        [HarmonyPrefix]
        [HarmonyPatch("FlyingPlants")]
        public static bool PreFlyingPlants(ref PlantType thePlantType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.FlyingPlants.Contains(thePlantType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.FlyingPlants.TryGetValue(thePlantType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("GetPlantTag")]
        public static bool PreGetPlantTag(ref Plant plant)
        {
            if (CustomCore.CustomPlantTypes.Contains(plant.thePlantType))
            {
                plant.plantTag = new()
                {
                    icePlant = TypeMgr.IsIcePlant(plant.thePlantType),
                    caltropPlant = TypeMgr.IsCaltrop(plant.thePlantType),
                    doubleBoxPlant = TypeMgr.DoubleBoxPlants(plant.thePlantType),
                    firePlant = TypeMgr.IsFirePlant(plant.thePlantType),
                    flyingPlant = TypeMgr.FlyingPlants(plant.thePlantType),
                    lanternPlant = TypeMgr.IsPlantern(plant.thePlantType),
                    smallLanternPlant = TypeMgr.IsSmallRangeLantern(plant.thePlantType),
                    magnetPlant = TypeMgr.IsMagnetPlants(plant.thePlantType),
                    nutPlant = TypeMgr.IsNut(plant.thePlantType),
                    tallNutPlant = TypeMgr.IsTallNut(plant.thePlantType),
                    potatoPlant = TypeMgr.IsPotatoMine(plant.thePlantType),
                    potPlant = TypeMgr.IsPot(plant.thePlantType),
                    puffPlant = TypeMgr.IsPuff(plant.thePlantType),
                    pumpkinPlant = TypeMgr.IsPumpkin(plant.thePlantType),
                    spickRockPlant = TypeMgr.IsSpickRock(plant.thePlantType),
                    tanglekelpPlant = TypeMgr.IsTangkelp(plant.thePlantType),
                    waterPlant = TypeMgr.IsWaterPlant(plant.thePlantType),
                };

                return false;
            }

            if (CustomCore.CustomPlantsSkin.ContainsKey(plant.thePlantType))
            {
                plant.plantTag = new()
                {
                    icePlant = TypeMgr.IsIcePlant(plant.thePlantType),
                    caltropPlant = TypeMgr.IsCaltrop(plant.thePlantType),
                    doubleBoxPlant = TypeMgr.DoubleBoxPlants(plant.thePlantType),
                    firePlant = TypeMgr.IsFirePlant(plant.thePlantType),
                    flyingPlant = TypeMgr.FlyingPlants(plant.thePlantType),
                    lanternPlant = TypeMgr.IsPlantern(plant.thePlantType),
                    smallLanternPlant = TypeMgr.IsSmallRangeLantern(plant.thePlantType),
                    magnetPlant = TypeMgr.IsMagnetPlants(plant.thePlantType),
                    nutPlant = TypeMgr.IsNut(plant.thePlantType),
                    tallNutPlant = TypeMgr.IsTallNut(plant.thePlantType),
                    potatoPlant = TypeMgr.IsPotatoMine(plant.thePlantType),
                    potPlant = TypeMgr.IsPot(plant.thePlantType),
                    puffPlant = TypeMgr.IsPuff(plant.thePlantType),
                    pumpkinPlant = TypeMgr.IsPumpkin(plant.thePlantType),
                    spickRockPlant = TypeMgr.IsSpickRock(plant.thePlantType),
                    tanglekelpPlant = TypeMgr.IsTangkelp(plant.thePlantType),
                    waterPlant = TypeMgr.IsWaterPlant(plant.thePlantType)
                };

                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsCaltrop")]
        public static bool PreIsCaltrop(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsCaltrop.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsCaltrop.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsFirePlant")]
        public static bool PreIsFirePlant(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsFirePlant.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsFirePlant.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsIcePlant")]
        public static bool PreIsIcePlant(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsIcePlant.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsIcePlant.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsMagnetPlants")]
        public static bool PreIsMagnetPlants(ref PlantType thePlantType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsMagnetPlants.Contains(thePlantType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsMagnetPlants.TryGetValue(thePlantType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsNut")]
        public static bool PreIsNut(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsNut.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsNut.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsPlantern")]
        public static bool PreIsPlantern(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsPlantern.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsPlantern.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsPot")]
        public static bool PreIsPot(ref PlantType thePlantType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsPot.Contains(thePlantType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsPot.TryGetValue(thePlantType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsPotatoMine")]
        public static bool PreIsPotatoMine(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsPotatoMine.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsPotatoMine.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsPuff")]
        public static bool PreIsPuff(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsPuff.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsPuff.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsPumpkin")]
        public static bool PreIsPumpkin(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsPumpkin.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsPumpkin.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsSmallRangeLantern")]
        public static bool PreIsSmallRangeLantern(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsSmallRangeLantern.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsSmallRangeLantern.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsSpecialPlant")]
        public static bool PreIsSpecialPlant(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsSpecialPlant.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsSpecialPlant.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsSpickRock")]
        public static bool PreIsSpickRock(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsSpickRock.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsSpickRock.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsTallNut")]
        public static bool PreIsTallNut(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsTallNut.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsTallNut.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsTangkelp")]
        public static bool PreIsTangkelp(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsTangkelp.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsTangkelp.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsWaterPlant")]
        public static bool PreIsWaterPlant(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsWaterPlant.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsWaterPlant.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("UmbrellaPlants")]
        public static bool PreUmbrellaPlants(ref PlantType thePlantType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.UmbrellaPlants.Contains(thePlantType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.UmbrellaPlants.TryGetValue(thePlantType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(UIMgr))]
    public static class UIMgrPatch
    {
        [HarmonyPatch(nameof(UIMgr.EnterChallengeMenu))]
        [HarmonyPostfix]
        public static void PostEnterChallengeMenu()
        {
            var levels = GameAPP.canvas.GetChild(0).FindChild("Levels");
            var firstBtns = levels.FindChild("FirstBtns");
            if (firstBtns.FindChild("CustomLevels") == null || firstBtns.FindChild("CustomLevels").IsDestroyed())
            {
                GameObject custom = UnityEngine.Object.Instantiate(firstBtns.GetChild(0).gameObject, firstBtns);
                custom.name = "CustomLevels";
                custom.transform.localPosition = new(-150, 30, 0);
                var window = custom.transform.FindChild("Window");
                window.FindChild("Name").GetComponent<TextMeshProUGUI>().text = "二创关卡";
                var adv = levels.FindChild("PageAdvantureLevel");
                var customLevels = UnityEngine.Object.Instantiate(adv.gameObject, levels);
                customLevels.active = false;
                customLevels.name = "PageCustomLevel";
                var pages = customLevels.transform.FindChild("Pages");
                var levelSample = UnityEngine.Object.Instantiate(pages.FindChild("Page1").FindChild("Lv1").gameObject);
                foreach (var l in pages.FindChild("Page1").GetComponentsInChildren<Transform>(true))
                {
                    UnityEngine.Object.Destroy(l.gameObject);
                }
                var pageSample = UnityEngine.Object.Instantiate(pages.FindChild("Page1").gameObject);
                UnityEngine.Object.Destroy(pages.FindChild("Page1").gameObject);
                UnityEngine.Object.Destroy(pages.FindChild("Page2").gameObject);
                UnityEngine.Object.Destroy(pages.FindChild("Page3").gameObject);
                int levelIndex = 0;
                int columnIndex = 0;
                int rowIndex = 0;
                int pageIndex = 0;
                foreach (var level in CustomCore.CustomLevels)
                {
                    if (levelIndex % 18 is 0)
                    {
                        UnityEngine.Object.Instantiate(pageSample, pages).name = $"Pages{levelIndex / 18 + 1}";
                    }
                    columnIndex = levelIndex % 6;
                    rowIndex = levelIndex / 6;
                    pageIndex = rowIndex / 3;
                    var levelBtn = UnityEngine.Object.Instantiate(levelSample, pages.FindChild($"Pages{levelIndex / 18 + 1}"));
                    levelBtn.transform.localPosition = new(-50 + 150 * columnIndex, 60 - 130 * rowIndex, 0);
                    levelBtn.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().sprite = level.Logo;
                    levelBtn.transform.GetChild(1).GetComponent<Advanture_Btn>().levelType = (LevelType)66;
                    levelBtn.transform.GetChild(1).GetComponent<Advanture_Btn>().buttonNumber = level.ID;
                    levelBtn.transform.GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>().text = level.Name();
                    levelIndex++;
                }
                window.GetComponent<FirstBtns>().pageToOpen = customLevels;
                window.GetComponent<FirstBtns>().originPosition = new(-150, 30, 0);
                UnityEngine.Object.Destroy(pageSample);
                UnityEngine.Object.Destroy(levelSample);
            }
        }

        [HarmonyPatch(nameof(UIMgr.EnterGame))]
        [HarmonyPrefix]
        public static bool PreEnterGame(ref LevelType levelType, ref int levelNumber, ref int id, ref string name)
        {
            if ((int)levelType is not 66) return true;
            var levelData = CustomCore.CustomLevels[levelNumber];

            // 清理UI资源
            GameAPP.UIManager.PopAll();

            // 重置相机
            CamaraFollowMouse.Instance.ResetCamera();

            // 设置游戏速度
            Time.timeScale = GameAPP.gameSpeed;

            // 设置当前关卡信息
            GameAPP.theBoardType = (LevelType)levelType;
            GameAPP.theBoardLevel = levelNumber;

            // 清理现有的Travel管理器
            if (TravelMgr.Instance != null)
            {
                UnityEngine.Object.Destroy(TravelMgr.Instance);
                TravelMgr.Instance = null;
            }

            // 创建游戏板
            GameObject boardGO = new("Board");
            GameAPP.board = boardGO;
            Board board = boardGO.AddComponent<Board>();
            board.boardTag = levelData.BoardTag;
            board.rowNum = levelData.RowCount;
            board.theMaxWave = levelData.WaveCount();
            board.cardSelectable = levelData.NeedSelectCard;
            board.theSun = levelData.Sun();
            board.zombieDamageAdder = levelData.ZombieHealthRate();
            board.seedPool = levelData.SeedRainPlantTypes().ToIl2CppList();
            levelData.PostBoard(board);
            // 加载并实例化地图
            GameObject mapInstance = UnityEngine.Object.Instantiate(MapData_cs.GetMap(levelData.SceneType, board), boardGO.transform);
            board.ChangeMap(mapInstance);

            InitZombieList.InitZombie(levelType, levelNumber, levelData.SceneType);

            // 播放音乐并开始游戏
            GameAPP.Instance.PlayMusic(MusicType.SelectCard);
            GameAPP.theGameStatus = GameStatus.InInterlude;

            // 初始化游戏板
            levelData.PreInitBoard();

            levelData.PostInitBoard(board.gameObject.AddComponent<InitBoard>());
            foreach (var p in levelData.PrePlants())
            {
                CreatePlant.Instance.SetPlant(p.Item1, p.Item2, p.Item3);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(WaveManager))]
    public static class WaveManagerPatch
    {
        [HarmonyPatch(nameof(WaveManager.GetMaxWave))]
        [HarmonyPostfix]
        public static void PostGetMaxWave(ref int __result)
        {
            if (Utils.IsCustomLevel(out var levelData))
            {
                __result = levelData.WaveCount();
            }
        }
    }

    /// <summary>
    /// 注册二创僵尸数据
    /// </summary>
    [HarmonyPatch(typeof(ZombieDataManager))]
    public static class ZombieDataPatch
    {
        [HarmonyPatch(nameof(ZombieDataManager.LoadData))]
        [HarmonyPostfix]
        public static void InitZombieData()
        {
            foreach (var z in CustomCore.CustomZombies)
            {
                ZombieDataManager.zombieDataDic[z.Key] = z.Value.Item3;
            }
        }
    }

    /// <summary>
    /// 子弹移动路径
    /// </summary>
    [HarmonyPatch(typeof(Bullet))]
    public static class BulletPatch
    {
        [HarmonyPatch(nameof(Bullet.PostionUpdate))]
        [HarmonyPrefix]
        public static bool PostionUpdate(Bullet __instance)
        {
            if (CustomCore.CustomBulletMovingWay.ContainsKey(__instance.theMovingWay))
            {
                CustomCore.CustomBulletMovingWay[__instance.theMovingWay](__instance);
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(SaveInfo))]
    public static class SaveInfoPatch_SaveLevelData
    {
        [HarmonyPatch(nameof(SaveInfo.SaveSurvivalData), new Type[] { typeof(int), typeof(bool), typeof(int) })]
        [HarmonyPostfix]
        public static void PostSaveSurvivalData_1(SaveInfo __instance, ref int level, ref int id)
        {
            if (TravelMgr.Instance == null)
                return;
            var array = (int[])TravelMgr.Instance.GetData("CustomBuffsLevel");
            if (array is null)
            {
                array = new int[CustomCore.CustomBuffsLevel.Count];
                TravelMgr.Instance.SetData("CustomBuffsLevel", array);
                return;
            }
            if (array.SequenceEqual(new int[CustomCore.CustomBuffsLevel.Count]))
                return;
            String json = JsonSerializer.Serialize(array);
            String originalPath = __instance.GetPath(level, id);
            String? directoryPath = Path.GetDirectoryName(originalPath);
            if (directoryPath is null)
                return;
            String fileName = Path.GetFileName(originalPath);
            String filePath = Path.Combine(directoryPath, $"{fileName}.extra.json");
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            File.WriteAllText(filePath, json);
        }

        [HarmonyPatch(nameof(SaveInfo.SaveSurvivalData), new Type[] { typeof(SurvivalData), typeof(int), typeof(int) })]
        [HarmonyPostfix]
        public static void PostSaveSurvivalData_2(SaveInfo __instance, ref int level, ref int id)
        {
            if (TravelMgr.Instance == null)
                return;
            var array = (int[])TravelMgr.Instance.GetData("CustomBuffsLevel");
            if (array is null)
            {
                array = new int[CustomCore.CustomBuffsLevel.Count];
                TravelMgr.Instance.SetData("CustomBuffsLevel", array);
                return;
            }
            if (array.SequenceEqual(new int[CustomCore.CustomBuffsLevel.Count]))
                return;
            String json = JsonSerializer.Serialize(array);
            String originalPath = __instance.GetPath(level, id);
            String? directoryPath = Path.GetDirectoryName(originalPath);
            if (directoryPath is null)
                return;
            String fileName = Path.GetFileName(originalPath);
            String filePath = Path.Combine(directoryPath, $"{fileName}.extra.json");
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            File.WriteAllText(filePath, json);
        }
    }

    [HarmonyPatch(typeof(SaveMgr))]
    public static class SaveMgrPatch
    {
        [HarmonyPatch(nameof(SaveMgr.LoadBoard))]
        [HarmonyPostfix]
        public static void PostLoadBoard(SaveMgr __instance, ref int level)
        {
            if (TravelMgr.Instance == null || SaveInfo.Instance == null)
                return;
            var idGet = SaveInfo.Instance.GetData("endlessID");
            if (idGet is null)
                return;
            var id = (int)idGet;
            String originalPath = SaveInfo.Instance.GetPath(level, id);
            String? directoryPath = Path.GetDirectoryName(originalPath);
            if (directoryPath is null)
                return;
            String fileName = Path.GetFileName(originalPath);
            String filePath = Path.Combine(directoryPath, $"{fileName}.extra.json");
            if (!File.Exists(filePath))
                return;
            String text = File.ReadAllText(filePath);
            int[]? array = JsonSerializer.Deserialize<int[]>(text);
            if (array is null)
                return;
            TravelMgr.Instance.SetData("CustomBuffsLevel", array);
            TravelMgr.Instance.SetData("LoadByEndless", true);
            SaveInfo.Instance.SetData("endlessID", null);
        }
    }

    [HarmonyPatch(typeof(TreasureData))]
    public static class TreasureDataPatch
    {
        [HarmonyPatch(nameof(TreasureData.GetCardLevel))]
        [HarmonyPrefix]
        public static bool GetCardLevel(TreasureData __instance, ref PlantType thePlantType, ref CardLevel __result)
        {
            if (CustomCore.TypeMgrExtra.LevelPlants.ContainsKey(thePlantType))
            {
                __result = CustomCore.TypeMgrExtra.LevelPlants[thePlantType];
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(UIMgr))]
    public static class UIMgrPatch_0
    {
        [HarmonyPatch(nameof(UIMgr.EnterGame))]
        [HarmonyPrefix]
        public static void PreEnterGame(UIMgr __instance, ref int levelNumber, ref int id, ref LevelType levelType)
        {
            if (SaveInfo.Instance == null)
                return;
            if (!Lawnf.IsTravelLevel(levelType, levelNumber))
                return;
            SaveInfo.Instance.SetData("endlessID", id);
        }
    }

    /*[HarmonyPatch(typeof(SaveInfo))]
    public static class SaveInfoPatch_SavePlant
    {
        [HarmonyPatch(nameof(SaveInfo.SaveSurvivalData), new Type[] { typeof(int), typeof(bool), typeof(int) })]
        [HarmonyPostfix]
        public static void PostSaveSurvivalData_1(SaveInfo __instance, ref int level, ref int id)
        {
            if (Board.Instance == null)
                return;
            var board = Board.Instance;
            if (board.plantArray is null)
                return;

            Dictionary<Plant, (Type, List<String>)> plants = new();
            List<Il2CppSystem.Type> types = CustomCore.CustomEndlessSave.Keys.Select(type => Il2CppType.From(type)).ToList();
            foreach (var plant in board.plantArray)
            {
                if (plant == null)
                    continue;
                if (plant.gameObject == null)
                    continue;
                var type = CustomCore.CustomEndlessSave.Keys.Select(type => (plant.gameObject.GetComponent(Il2CppType.From(type)) != null) ? type : null).ToList();
                if (type.Count == 0)
                    continue;
                foreach (var item in type)
                {
                    if (item == null)
                        continue;
                    if (plants.ContainsKey(plant))
                        if (CustomCore.CustomEndlessSave.ContainsKey(item))
                            foreach (var i in CustomCore.CustomEndlessSave[item])
                                if (!plants[plant].Item2.Contains(i))
                                    plants[plant].Item2.Add(i);
                    else
                        if (CustomCore.CustomEndlessSave.ContainsKey(item))
                            plants.Add(plant, (item, CustomCore.CustomEndlessSave[item]));
                }

            }

            List<CustomPlantSaveData> datas = new();

            foreach (var (plant, list) in plants)
            {
                if (plant == null)
                    continue;

                var data = new CustomPlantSaveData();
                data.thePlantType = (int)plant.thePlantType;
                data.thePlantRow = plant.thePlantRow;
                data.thePlantColumn = plant.thePlantColumn;

                var type = list.Item1;
                foreach (var item in list.Item2)
                {
                    var propertyInfo = type.GetProperty(item);
                    if (propertyInfo is null)
                        continue;
                    data.data.Add(new CustomPlantEndlessData()
                    {
                        Name = item,
                        Value = propertyInfo.GetValue(plant)
                    });
                }

                datas.Add(data);
            }

            String json = JsonSerializer.Serialize(datas);
            String originalPath = __instance.GetPath(level, id);
            String? directoryPath = Path.GetDirectoryName(originalPath);
            if (directoryPath is null)
                return;
            String fileName = Path.GetFileName(originalPath);
            String filePath = Path.Combine(directoryPath, $"{fileName}.plantData.json");
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            File.WriteAllText(filePath, json);
        }
    }*/
}