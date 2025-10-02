using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CustomizeLib.BepInEx
{
    public static class ExtensionData
    {
        public static Dictionary<Type, Dictionary<String, object>> staticData { get; set; } = [];
        public static Dictionary<Type, Dictionary<object, Dictionary<String, object>>> instanceData { get; set; } = [];

        /// <summary>
        /// 获取扩展数据
        /// </summary>
        /// <param name="component"></param>
        /// <param name="name">数据名</param>
        /// <returns>数据值</returns>
        /// <exception cref="ArgumentException"></exception>
        public static object GetData(this Component component, String name) => component.gameObject.GetData(name);

        /// <summary>
        /// 设置扩展数据
        /// </summary>
        /// <param name="component"></param>
        /// <param name="name">数据名</param>
        /// <param name="data">数据值</param>
        public static void SetData(this Component component, String name, object data) => component.gameObject.SetData(name, data);

        /// <summary>
        /// 获取扩展数据
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="name">数据名</param>
        /// <returns>数据值</returns>
        /// <exception cref="ArgumentException"></exception>
        public static object GetData(this GameObject gameObject, String name)
        {
            if (gameObject.TryGetComponent<ExtensionDataComponent>(out var edc))
                return edc.GetData(name);
            else
            {
                gameObject.AddComponent<ExtensionDataComponent>();
                throw new ArgumentException("No data with name " + name);
            }
        }

        /// <summary>
        /// 设置扩展数据
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="name">数据名</param>
        /// <param name="data">数据值</param>
        public static void SetData(this GameObject gameObject, String name, object data)
        {
            if (gameObject.TryGetComponent<ExtensionDataComponent>(out var edc))
                edc.SetData(name, data);
            else
            {
                var result = gameObject.AddComponent<ExtensionDataComponent>();
                result.SetData(name, data);
            }
        }

        /// <summary>
        /// 设置类扩展数据
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="name">数据名</param>
        /// <param name="data">数据值</param>
        public static void SetData<T>(String name, object data) => SetData(typeof(T), name, data);

        /// <summary>
        /// 设置类扩展数据
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="name">数据名</param>
        /// <param name="data">数据值</param>
        public static void SetData(Type type, String name, object data)
        {
            if (staticData.ContainsKey(type))
                if (staticData[type].ContainsKey(name))
                    staticData[type][name] = data;
                else
                    staticData[type].Add(name, data);
            else
                staticData.Add(type, new Dictionary<String, object>() { { name, data } });
        }

        /// <summary>
        /// 获取类扩展数据
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="name">数据名</param>
        /// <returns>数据值</returns>
        public static object GetData<T>(String name) => GetData(typeof(T), name);

        /// <summary>
        /// 获取类扩展数据
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="name">数据名</param>
        /// <returns>数据值</returns>
        /// <exception cref="ArgumentException"></exception>
        public static object GetData(Type type, String name)
        {
            if (staticData.ContainsKey(type))
                if (staticData[type].ContainsKey(name))
                    return staticData[type][name];
                else
                    throw new ArgumentException("No data with name " + name + " class " + type);
            else
                throw new ArgumentException("No data with name " + name);
        }

        /// <summary>
        /// 获取实例扩展数据
        /// </summary>
        /// <param name="obj">实例</param>
        /// <param name="name">数据名</param>
        /// <param name="data">数据值</param>
        public static void SetData(this object obj, String name, object data)
        {
            if (instanceData.ContainsKey(obj.GetType()))
                if (instanceData[obj.GetType()].ContainsKey(obj))
                    if (instanceData[obj.GetType()][obj].ContainsKey(name))
                        instanceData[obj.GetType()][obj][name] = data;
                    else
                        instanceData[obj.GetType()][obj].Add(name, data);
                else
                    instanceData[obj.GetType()].Add(obj, new Dictionary<String, object>() { { name, data } });
            else
                instanceData.Add(obj.GetType(), new Dictionary<object, Dictionary<String, object>>() { { obj, new Dictionary<String, object>() { { name, data } } } });
        }

        /// <summary>
        /// 获取实例扩展数据
        /// </summary>
        /// <param name="obj">实例</param>
        /// <param name="name">数据名</param>
        /// <returns>数据值</returns>
        /// <exception cref="ArgumentException"></exception>
        public static object GetData(this object obj, String name)
        {
            if (instanceData.ContainsKey(obj.GetType()))
                if (instanceData[obj.GetType()].ContainsKey(obj))
                    if (instanceData[obj.GetType()][obj].ContainsKey(name))
                        return instanceData[obj.GetType()][obj][name];
                    else
                        throw new ArgumentException("No data with name " + name + " class " + obj.GetType());
                else
                    throw new ArgumentException("No data with name " + name);
            else
                throw new ArgumentException("No data with name " + name);
        }
    }

    public class ExtensionDataComponent : MonoBehaviour
    {
        public Dictionary<String, object> data { get; set; } = [];

        public object GetData(String name)
        {
            if (data.ContainsKey(name))
                return data[name];
            else
                throw new ArgumentException("No data with name " + name);
        }

        public void SetData(String name, object data)
        {
            if (this.data.ContainsKey(name))
                this.data[name] = data;
            else
                this.data.Add(name, data);
        }
    }
}
