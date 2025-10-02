// 没写完，所以bep暂不做支持

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.Injection;
using System.IO;
using Il2CppInterop.Common;
using Il2CppInterop.Runtime.Startup;
using System.Collections;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#pragma warning disable
namespace CustomizeLib.MelonLoader
{
    public static class Il2CppObjectToSystem
    {
        private static readonly Dictionary<Type, MethodInfo> UnboxCache = new Dictionary<Type, MethodInfo>();

        /// <summary>
        /// 将 Il2CppSystem.Object 转换为指定类型
        /// </summary>
        public static T ConvertTo<T>(this Il2CppSystem.Object obj)
        {
            return (T)ConvertTo(obj, typeof(T));
        }

        /// <summary>
        /// 将 Il2CppSystem.Object 转换为指定类型
        /// </summary>
        public static object ConvertTo(this Il2CppSystem.Object obj, Type targetType)
        {
            if (obj == null)
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

            if (targetType == typeof(Il2CppSystem.Object) || targetType == typeof(object))
                return obj;

            // 特殊处理数组类型
            if (targetType.IsArray || targetType == typeof(Array))
                return ConvertToArray(obj, targetType);

            // 检查是否是 Il2Cpp 类型
            if (typeof(Il2CppObjectBase).IsAssignableFrom(targetType))
                return ConvertToIl2CppType(obj, targetType);

            var actualType = obj.GetType();

            // 如果对象已经是目标类型，直接返回
            if (targetType.IsAssignableFrom(actualType))
                return obj;

            // 处理字符串类型
            if (targetType == typeof(string))
                return obj.ToString();

            // 处理值类型转换
            if (targetType.IsValueType)
                return ConvertToValueType(obj, targetType);

            // 处理引用类型转换
            return ConvertToReferenceType(obj, targetType);
        }

        /// <summary>
        /// 处理引用类型转换
        /// </summary>
        private static object ConvertToReferenceType(Il2CppSystem.Object obj, Type targetType)
        {
            try
            {
                // 尝试使用 ChangeType
                return Convert.ChangeType(obj, targetType);
            }
            catch
            {
                // 如果 ChangeType 失败，尝试直接转换
                try
                {
                    if (typeof(Il2CppObjectBase).IsAssignableFrom(targetType))
                        return ConvertToIl2CppType(obj, targetType);

                    return obj;
                }
                catch (Exception ex)
                {
                    throw new InvalidCastException($"Cannot convert {obj.GetType()} to {targetType}", ex);
                }
            }
        }

        /// <summary>
        /// 转换到 Il2Cpp 类型
        /// </summary>
        private static object ConvertToIl2CppType(Il2CppSystem.Object obj, Type targetType)
        {
            try
            {
                // 使用指针方式转换
                var constructor = targetType.GetConstructor(new Type[] { typeof(IntPtr) });
                if (constructor != null)
                {
                    return constructor.Invoke(new object[] { obj.Pointer });
                }

                // 尝试使用静态 TryCast 方法
                var tryCastMethod = targetType.GetMethod("TryCast", BindingFlags.Static | BindingFlags.Public);
                if (tryCastMethod != null)
                {
                    return tryCastMethod.Invoke(null, new object[] { obj });
                }

                throw new InvalidCastException($"No suitable conversion method found for {targetType}");
            }
            catch (Exception ex)
            {
                throw new InvalidCastException($"Cannot convert {obj.GetType()} to {targetType}", ex);
            }
        }

        /// <summary>
        /// 处理值类型转换
        /// </summary>
        private static object ConvertToValueType(Il2CppSystem.Object obj, Type targetType)
        {
            // 处理可空类型
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var underlyingType = Nullable.GetUnderlyingType(targetType);
                var value = ConvertTo(obj, underlyingType);
                return Activator.CreateInstance(targetType, value);
            }

            // 处理枚举类型
            if (targetType.IsEnum)
            {
                var underlyingType = Enum.GetUnderlyingType(targetType);
                var underlyingValue = ConvertTo(obj, underlyingType);
                return Enum.ToObject(targetType, underlyingValue);
            }

            // 尝试使用 UnBox 方法处理基本类型
            try
            {
                // 检查是否是基本类型
                if (IsUnmanagedType(targetType))
                {
                    return UnboxValue(obj, targetType);
                }

                // 对于非基本类型，使用备用方法
                return FallbackConvert(obj, targetType);
            }
            catch
            {
                // 如果 UnBox 失败，尝试其他转换方式
                return FallbackConvert(obj, targetType);
            }
        }

        /// <summary>
        /// 检查类型是否为非托管类型
        /// </summary>
        private static bool IsUnmanagedType(Type type)
        {
            return type.IsPrimitive || type.IsEnum ||
                   type == typeof(decimal) || type == typeof(IntPtr) || type == typeof(UIntPtr);
        }

        /// <summary>
        /// 使用 UnBox 方法解箱值
        /// </summary>
        private static object UnboxValue(Il2CppSystem.Object obj, Type targetType)
        {
            try
            {
                // 使用反射调用适当的 UnBox 方法
                var unboxMethod = typeof(Il2CppObjectToSystem).GetMethod("UnboxGeneric",
                    BindingFlags.NonPublic | BindingFlags.Static);
                var genericMethod = unboxMethod.MakeGenericMethod(targetType);
                return genericMethod.Invoke(null, new object[] { obj });
            }
            catch (Exception ex)
            {
                throw new InvalidCastException($"Cannot unbox {obj} to {targetType}", ex);
            }
        }

        /// <summary>
        /// 通用解箱方法
        /// </summary>
        private static object UnboxGeneric<T>(Il2CppSystem.Object obj) where T : unmanaged
        {
            try
            {
                // 使用 Il2CppObjectBoxer.Unbox 方法
                return obj.Unbox<T>();
            }
            catch
            {
                // 如果 Unbox 失败，尝试备用方法
                return FallbackUnbox<T>(obj);
            }
        }

        /// <summary>
        /// 备用的解箱方法
        /// </summary>
        private static T FallbackUnbox<T>(Il2CppSystem.Object obj) where T : unmanaged
        {
            // 对于基本类型，使用 Marshal 读取值
            if (typeof(T) == typeof(int))
                return (T)(object)Marshal.ReadInt32(obj.Pointer);
            if (typeof(T) == typeof(float))
            {
                int intValue = Marshal.ReadInt32(obj.Pointer);
                return (T)(object)BitConverter.ToSingle(BitConverter.GetBytes(intValue), 0);
            }
            if (typeof(T) == typeof(double))
            {
                long longValue = Marshal.ReadInt64(obj.Pointer);
                return (T)(object)BitConverter.Int64BitsToDouble(longValue);
            }
            if (typeof(T) == typeof(bool))
                return (T)(object)(Marshal.ReadByte(obj.Pointer) != 0);
            if (typeof(T) == typeof(short))
                return (T)(object)Marshal.ReadInt16(obj.Pointer);
            if (typeof(T) == typeof(long))
                return (T)(object)Marshal.ReadInt64(obj.Pointer);
            if (typeof(T) == typeof(byte))
                return (T)(object)Marshal.ReadByte(obj.Pointer);
            if (typeof(T) == typeof(char))
                return (T)(object)(char)Marshal.ReadInt16(obj.Pointer);
            if (typeof(T) == typeof(uint))
                return (T)(object)(uint)Marshal.ReadInt32(obj.Pointer);
            if (typeof(T) == typeof(ushort))
                return (T)(object)(ushort)Marshal.ReadInt16(obj.Pointer);
            if (typeof(T) == typeof(ulong))
                return (T)(object)(ulong)Marshal.ReadInt64(obj.Pointer);
            if (typeof(T) == typeof(sbyte))
                return (T)(object)(sbyte)Marshal.ReadByte(obj.Pointer);

            return default(T);
        }

        /// <summary>
        /// 备用转换方法
        /// </summary>
        private static object FallbackConvert(Il2CppSystem.Object obj, Type targetType)
        {
            // 尝试通过字符串中转进行转换
            try
            {
                var stringValue = obj.ToString();
                if (targetType == typeof(Guid))
                    return Guid.Parse(stringValue);

                if (targetType == typeof(DateTime))
                    return DateTime.Parse(stringValue);

                if (targetType == typeof(TimeSpan))
                    return TimeSpan.Parse(stringValue);

                // 使用 TypeDescriptor 进行转换
                var converter = System.ComponentModel.TypeDescriptor.GetConverter(targetType);
                if (converter != null && converter.CanConvertFrom(typeof(string)))
                {
                    return converter.ConvertFromString(stringValue);
                }
            }
            catch
            {
                // 忽略错误，尝试其他方法
            }

            // 尝试使用 TryCast 方法转换 Il2Cpp 类型
            if (typeof(Il2CppObjectBase).IsAssignableFrom(targetType))
            {
                try
                {
                    return ConvertToIl2CppType(obj, targetType);
                }
                catch
                {
                    // 忽略错误，继续抛出下面的异常
                }
            }

            throw new InvalidCastException($"Cannot convert {obj.GetType()} to {targetType}");
        }

        /// <summary>
        /// 处理数组转换
        /// </summary>
        private static object ConvertToArray(Il2CppSystem.Object obj, Type targetType)
        {
            // 获取数组元素类型
            Type elementType = targetType.GetElementType();
            if (elementType == null && targetType == typeof(Array))
            {
                elementType = typeof(object);
            }

            // 尝试转换为 Il2CppSystem.Array
            Il2CppSystem.Array il2cppArray = obj as Il2CppSystem.Array;

            // 如果不是直接是数组，尝试使用指针方式转换
            if (il2cppArray == null)
            {
                try
                {
                    // 尝试使用指针方式创建数组
                    var arrayType = typeof(Il2CppSystem.Array);
                    var constructor = arrayType.GetConstructor(new Type[] { typeof(IntPtr) });
                    if (constructor != null)
                    {
                        il2cppArray = (Il2CppSystem.Array)constructor.Invoke(new object[] { obj.Pointer });
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidCastException($"Cannot convert {obj.GetType()} to array type {targetType}", ex);
                }
            }

            if (il2cppArray == null)
            {
                throw new InvalidCastException($"Cannot convert {obj.GetType()} to array type {targetType}");
            }

            // 处理多维数组
            int rank = il2cppArray.Rank;
            if (rank > 1)
            {
                return ConvertMultidimensionalArray(il2cppArray, elementType);
            }

            // 处理一维数组
            var length = il2cppArray.Length;
            var result = Array.CreateInstance(elementType, length);

            for (int i = 0; i < length; i++)
            {
                var element = il2cppArray.GetValue(i);
                object convertedElement;

                if (element is Il2CppSystem.Object il2cppElement)
                {
                    convertedElement = ConvertTo(il2cppElement, elementType);
                }
                else
                {
                    try
                    {
                        convertedElement = ConvertSimpleType(element, elementType);
                    }
                    catch
                    {
                        convertedElement = element;
                    }
                }

                result.SetValue(convertedElement, i);
            }

            return result;
        }

        /// <summary>
        /// 转换多维数组
        /// </summary>
        private static object ConvertMultidimensionalArray(Il2CppSystem.Array il2cppArray, Type elementType)
        {
            // 获取数组的维度信息
            int rank = il2cppArray.Rank;
            int[] lengths = new int[rank];
            int[] lowerBounds = new int[rank];

            for (int i = 0; i < rank; i++)
            {
                lengths[i] = il2cppArray.GetLength(i);
                lowerBounds[i] = il2cppArray.GetLowerBound(i);
            }

            // 创建目标多维数组
            Array result = Array.CreateInstance(elementType, lengths, lowerBounds);

            // 使用迭代处理多维数组
            ConvertMultidimensionalArrayIterative(il2cppArray, result, elementType);

            return result;
        }

        /// <summary>
        /// 迭代转换多维数组
        /// </summary>
        private static void ConvertMultidimensionalArrayIterative(Il2CppSystem.Array source, Array target, Type elementType)
        {
            int rank = source.Rank;
            int totalElements = 1;

            for (int i = 0; i < rank; i++)
            {
                totalElements *= source.GetLength(i);
            }

            // 使用平面索引迭代
            for (int flatIndex = 0; flatIndex < totalElements; flatIndex++)
            {
                // 将平面索引转换为多维索引
                int[] indices = new int[rank];
                int temp = flatIndex;

                for (int i = rank - 1; i >= 0; i--)
                {
                    indices[i] = temp % source.GetLength(i);
                    temp /= source.GetLength(i);
                }

                // 获取元素
                object element = source.GetValue(indices);
                object convertedElement;

                if (element is Il2CppSystem.Object il2cppElement)
                {
                    convertedElement = ConvertTo(il2cppElement, elementType);
                }
                else
                {
                    try
                    {
                        convertedElement = ConvertSimpleType(element, elementType);
                    }
                    catch
                    {
                        convertedElement = element;
                    }
                }

                target.SetValue(convertedElement, indices);
            }
        }

        /// <summary>
        /// 简单类型转换
        /// </summary>
        private static object ConvertSimpleType(object value, Type targetType)
        {
            if (value == null)
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

            if (targetType.IsAssignableFrom(value.GetType()))
                return value;

            // 处理基本类型转换
            if (targetType == typeof(int))
                return Convert.ToInt32(value);
            if (targetType == typeof(float))
                return Convert.ToSingle(value);
            if (targetType == typeof(double))
                return Convert.ToDouble(value);
            if (targetType == typeof(bool))
                return Convert.ToBoolean(value);
            if (targetType == typeof(string))
                return Convert.ToString(value);
            if (targetType == typeof(short))
                return Convert.ToInt16(value);
            if (targetType == typeof(long))
                return Convert.ToInt64(value);
            if (targetType == typeof(byte))
                return Convert.ToByte(value);
            if (targetType == typeof(char))
                return Convert.ToChar(value);
            if (targetType == typeof(decimal))
                return Convert.ToDecimal(value);
            if (targetType == typeof(uint))
                return Convert.ToUInt32(value);
            if (targetType == typeof(ushort))
                return Convert.ToUInt16(value);
            if (targetType == typeof(ulong))
                return Convert.ToUInt64(value);
            if (targetType == typeof(sbyte))
                return Convert.ToSByte(value);

            // 对于其他类型，尝试使用 ChangeType
            return Convert.ChangeType(value, targetType);
        }

        // 添加特定类型的转换方法
        public static int ToInt(this Il2CppSystem.Object obj) => ConvertTo<int>(obj);
        public static float ToFloat(this Il2CppSystem.Object obj) => ConvertTo<float>(obj);
        public static bool ToBool(this Il2CppSystem.Object obj) => ConvertTo<bool>(obj);
        public static string ToStringExt(this Il2CppSystem.Object obj, bool useOriginal = false)
            => useOriginal ? obj.ToString() : ConvertTo<string>(obj);

        // 添加数组转换的便捷方法
        public static Array ToArray(this Il2CppSystem.Object obj) => ConvertTo<Array>(obj);
        public static T[] ToArray<T>(this Il2CppSystem.Object obj) => ConvertTo<T[]>(obj);
        public static T[,] ToArray2D<T>(this Il2CppSystem.Object obj) => ConvertTo<T[,]>(obj);

        // 添加 TryCast 扩展方法
        public static T TryCast<T>(this Il2CppSystem.Object obj) where T : Il2CppObjectBase
        {
            if (obj == null) return null;

            try
            {
                // 使用指针方式转换
                var constructor = typeof(T).GetConstructor(new Type[] { typeof(IntPtr) });
                if (constructor != null)
                {
                    return (T)constructor.Invoke(new object[] { obj.Pointer });
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // 添加安全的 UnBox 方法
        public static T Unbox<T>(this Il2CppSystem.Object obj) where T : unmanaged
        {
            return (T)UnboxGeneric<T>(obj);
        }

        /// <summary>
        /// 调试方法：输出数组的详细信息
        /// </summary>
        public static void DebugArrayInfo(Il2CppSystem.Array il2cppArray, string filePath)
        {
            if (il2cppArray == null) return;

            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, false))
                {
                    writer.WriteLine($"Array Type: {il2cppArray.GetType()}");
                    writer.WriteLine($"Rank: {il2cppArray.Rank}");
                    writer.WriteLine($"Length: {il2cppArray.Length}");

                    int rank = il2cppArray.Rank;
                    for (int i = 0; i < rank; i++)
                    {
                        writer.WriteLine($"Dimension {i}: Length={il2cppArray.GetLength(i)}, LowerBound={il2cppArray.GetLowerBound(i)}");
                    }

                    writer.WriteLine("=== Array Contents ===");

                    // 对于二维数组
                    if (rank == 2)
                    {
                        int length0 = il2cppArray.GetLength(0);
                        int length1 = il2cppArray.GetLength(1);

                        // 只输出前10x10的元素，避免文件过大
                        for (int i = 0; i < Math.Min(10, length0); i++)
                        {
                            for (int j = 0; j < Math.Min(10, length1); j++)
                            {
                                int[] indices = new int[] { i, j };
                                object element = il2cppArray.GetValue(indices);

                                writer.WriteLine($"[{i}, {j}]: {element} (Type: {element?.GetType()})");

                                // 如果是 Il2CppSystem.Object，输出更多信息
                                if (element is Il2CppSystem.Object il2cppElement)
                                {
                                    writer.WriteLine($"  Pointer: {il2cppElement.Pointer}");
                                    writer.WriteLine($"  ToString(): {il2cppElement.ToString()}");

                                    // 尝试获取字段信息
                                    try
                                    {
                                        var fields = il2cppElement.GetType().GetFields(
                                            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                                        foreach (var field in fields)
                                        {
                                            try
                                            {
                                                var value = field.GetValue(il2cppElement);
                                                writer.WriteLine($"  Field {field.Name}: {value} (Type: {field.FieldType})");
                                            }
                                            catch
                                            {
                                                writer.WriteLine($"  Field {field.Name}: <无法获取值>");
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        writer.WriteLine("  无法获取字段信息");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 忽略错误
            }
        }
    }

    public static class Il2CppConversionHelper
    {
        /// <summary>
        /// 安全地将IL2CPP数组转换为托管数组
        /// </summary>
        public static T[] ConvertFromIl2CppArray<T>(Il2CppSystem.Array il2CppArray)
        {
            if (il2CppArray == null)
                return null;

            int length = il2CppArray.Length;
            T[] managedArray = new T[length];

            for (int i = 0; i < length; i++)
            {
                object element = il2CppArray.GetValue(i);

                // 特殊处理：如果T是string且元素是Il2CppSystem.String
                if (typeof(T) == typeof(string) && element is Il2CppSystem.String il2CppString)
                {
                    managedArray[i] = (T)(object)il2CppString.ToString();
                }
                // 特殊处理：如果元素是Il2CppStructArray<char>且T是string
                else if (typeof(T) == typeof(string) && element is Il2CppStructArray<char> charArray)
                {
                    managedArray[i] = (T)(object)ConvertCharArrayToString(charArray);
                }
                else
                {
                    // 尝试直接转换或使用ChangeType
                    try
                    {
                        managedArray[i] = (T)element;
                    }
                    catch (InvalidCastException)
                    {
                        managedArray[i] = (T)Convert.ChangeType(element, typeof(T));
                    }
                }
            }

            return managedArray;
        }

        /// <summary>
        /// 安全地将托管数组转换为IL2CPP数组
        /// </summary>
        public static Il2CppReferenceArray<T> ConvertToIl2CppArray<T>(T[] managedArray) where T : Il2CppObjectBase
        {
            if (managedArray == null)
                return null;

            int length = managedArray.Length;
            Il2CppReferenceArray<T> il2CppArray = new Il2CppReferenceArray<T>(length);

            for (int i = 0; i < length; i++)
            {
                il2CppArray[i] = managedArray[i];
            }

            return il2CppArray;
        }

        /// <summary>
        /// 将字符串转换为Il2CppStructArray<char>
        /// </summary>
        public static Il2CppStructArray<char> ConvertStringToCharArray(string str)
        {
            if (str == null)
                return null;

            Il2CppStructArray<char> charArray = new Il2CppStructArray<char>(str.Length);

            for (int i = 0; i < str.Length; i++)
            {
                charArray[i] = str[i];
            }

            return charArray;
        }

        /// <summary>
        /// 将Il2CppStructArray<char>转换为字符串
        /// </summary>
        public static string ConvertCharArrayToString(Il2CppStructArray<char> charArray)
        {
            if (charArray == null)
                return null;

            char[] managedChars = new char[charArray.Length];
            for (int i = 0; i < charArray.Length; i++)
            {
                managedChars[i] = charArray[i];
            }

            return new string(managedChars);
        }

        /// <summary>
        /// 安全复制数组元素（处理复杂类型）
        /// </summary>
        public static void SafeArrayCopy(Il2CppSystem.Array source, Array destination, int length)
        {
            for (int i = 0; i < length; i++)
            {
                object value = source.GetValue(i);
                destination.SetValue(value, i);
            }
        }
    }
}