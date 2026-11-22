using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Utilities;
using Sims3.Metadata;
using Sims3.SimIFace;

namespace Destrospean.Utils
{
    public static class CommonUtils
    {
        private const string kAuthorName = "Destrospean";

        public static void AddEnumValue<T>(string key, object value) where T : struct
        {
            var enumType = typeof(T);
            if (!ParserFunctions.sCaseInsensitiveEnumParsers.TryGetValue(enumType, out var caseInsensitiveEnumParser))
            {
                caseInsensitiveEnumParser = new EnumParser(enumType, true);
                ParserFunctions.sCaseInsensitiveEnumParsers.Add(enumType, caseInsensitiveEnumParser);
            }
            if (!ParserFunctions.sCaseSensitiveEnumParsers.TryGetValue(enumType, out var caseSensitiveEnumParser))
            {
                caseSensitiveEnumParser = new EnumParser(enumType, false);
                ParserFunctions.sCaseSensitiveEnumParsers.Add(enumType, caseSensitiveEnumParser);
            }
            if (!caseInsensitiveEnumParser.mLookup.ContainsKey(key.ToLowerInvariant()) && !caseSensitiveEnumParser.mLookup.ContainsKey(key))
            {
                caseInsensitiveEnumParser.mLookup.Add(key.ToLowerInvariant(), value);
                caseSensitiveEnumParser.mLookup.Add(key, value);
            }
        }

        /// <summary>
        /// Gets the localization key from the class name and namespace of the object type.
        /// </summary>
        /// <returns>The localization key.</returns>
        /// <param name="type">Type.</param>
        public static string GetLocalizationKey(this Type type)
        {
            return type.Namespace?.Substring(type.Namespace.IndexOf(kAuthorName, StringComparison.Ordinal)).Replace('.', '/') + "/" + type.Name;
        }
        
        public static T GetRandomItem<T>(this IEnumerable<T> enumerable)
        {
            var list = new List<T>(enumerable);
            return list[RandomUtil.GetInt(list.Count - 1)];
        }
        
        public static void Read(this IPropertyStreamReader reader, object memberHolder, string memberName, object defaultValue)
        {
            var field = memberHolder.GetType().GetField(memberName, (BindingFlags)0x34);
            var property = memberHolder.GetType().GetProperty(memberName, (BindingFlags)0x34);
            var nameHash = ResourceUtils.HashString32(memberName);
            object value = null;
            switch ((field?.GetType() ?? property?.GetType())?.Name)
            {
                case nameof(Boolean):
                    reader.ReadBool(nameHash, out var boolVal, Convert.ToBoolean(defaultValue));
                    value = boolVal;
                    break;
                case nameof(Byte):
                    reader.ReadByte(nameHash, out var byteVal, Convert.ToByte(defaultValue));
                    value = byteVal;
                    break;
                case nameof(Int16):
                    reader.ReadInt16(nameHash, out var int16Val, Convert.ToInt16(defaultValue));
                    value = int16Val;
                    break;
                case nameof(Int32):
                    reader.ReadInt32(nameHash, out var int32Val, Convert.ToInt32(defaultValue));
                    value = int32Val;
                    break;
                case nameof(Int64):
                    reader.ReadInt64(nameHash, out var int64Val, Convert.ToInt64(defaultValue));
                    value = int64Val;
                    break;
                case nameof(Single):
                    reader.ReadFloat(nameHash, out var floatVal, Convert.ToSingle(defaultValue));
                    value = floatVal;
                    break;
                case nameof(String):
                    reader.ReadString(nameHash, out var stringVal, Convert.ToString(defaultValue));
                    value = stringVal;
                    break;
                case nameof(UInt16):
                    reader.ReadUint16(nameHash, out var uint16Val, Convert.ToUInt16(defaultValue));
                    value = uint16Val;
                    break;
                case nameof(UInt32):
                    reader.ReadUint32(nameHash, out var uint32Val, Convert.ToUInt32(defaultValue));
                    value = uint32Val;
                    break;
                case nameof(UInt64):
                    reader.ReadUint64(nameHash, out var uint64Val, Convert.ToUInt64(defaultValue));
                    value = uint64Val;
                    break;
            }
            if (value != null)
            {
                field?.SetValue(memberHolder, value);
                property?.SetValue(memberHolder, value, null);
            }
        }

        public static void RemoveEnumValue<T>(string key) where T : struct
        {
            if (ParserFunctions.sCaseInsensitiveEnumParsers.TryGetValue(typeof(T), out var caseInsensitiveEnumParser) && caseInsensitiveEnumParser.mLookup.ContainsKey(key.ToLowerInvariant()))
            {
                caseInsensitiveEnumParser.mLookup.Remove(key.ToLowerInvariant());
            }
            if (ParserFunctions.sCaseSensitiveEnumParsers.TryGetValue(typeof(T), out var caseSensitiveEnumParser))
            {
                caseSensitiveEnumParser.mLookup.Remove(key);
            }
        }

        /// <summary>
        /// Replaces a method (and its overloads) with another method.
        /// Note: only the overloads defined in the new type will replace the corresponding overloads of the old type.
        /// </summary>
        /// <typeparam name="OldType">The class that holds the method to be replaced.</typeparam>
        /// <typeparam name="NewType">The class that holds the new method. Note: only the overloads defined in the new type will replace the corresponding overloads of the old type.</typeparam>
        public static void ReplaceMethod<OldType, NewType>(string methodName)
        {
            ReplaceMethod<OldType, NewType>(methodName, methodName);
        }

        /// <summary>
        /// This method was borrowed from Lazy Duchess' Mono Patcher.
        /// </summary>
        public static void ReplaceMethod(MethodInfo oldMethod, MethodInfo newMethod)
        {
            var replacementByteArray = new byte[40];
            Marshal.Copy(newMethod.MethodHandle.Value, replacementByteArray, 0, 40);
            Marshal.Copy(replacementByteArray, 0, oldMethod.MethodHandle.Value, 24);
            Marshal.Copy(replacementByteArray, 28, new IntPtr(oldMethod.MethodHandle.Value.ToInt32() + 28), 12);
        }

        /// <summary>
        /// Replaces a method (and its overloads) with another method.
        /// Note: only the overloads defined in the new type will replace the corresponding overloads of the old type.
        /// </summary>
        /// <param name="oldMethodName">The name of the method to be replaced.</param>
        /// <param name="newMethodName">The name of the new method.</param>
        /// <typeparam name="OldType">The class that holds the method to be replaced.</typeparam>
        /// <typeparam name="NewType">The class that holds the new method. Note: only the overloads defined in the new type will replace the corresponding overloads of the old type.</typeparam>
        public static void ReplaceMethod<OldType, NewType>(string oldMethodName, string newMethodName)
        {
            foreach (var method in typeof(NewType).GetMethods((BindingFlags)0x3C))
            {
                if (method.Name == newMethodName)
                {
                    var oldMethod = typeof(OldType).GetMethod(oldMethodName, (BindingFlags)0x3C, null, Array.ConvertAll(method.GetParameters(), x => x.ParameterType), null);
                    if (oldMethod != null)
                    {
                        ReplaceMethod(oldMethod, method);
                    }
                }
            }
        }

        public static bool TryChangeScriptClass(this GameObject gameObject, string newScriptClassName)
        {
            if (string.IsNullOrEmpty(newScriptClassName))
            {
                return false;
            }
            var resourceKey = gameObject.GetResourceKey();
            var newGameObject = GlobalFunctions.CreateObject(new ResourceKey(resourceKey.InstanceId, 0x319E4F1D, resourceKey.GroupId), gameObject.Position, gameObject.Level, gameObject.ForwardVector, newScriptClassName, null) as GameObject;
            if (newGameObject == null || newGameObject is FailureObject)
            {
                return false;
            }
            var enabledStencils = new SortedList<string, bool>(StringComparer.Ordinal);
            Complate.SetupDesignSwap(newGameObject.ObjectId, Complate.ExtractPatterns(gameObject.ObjectId, enabledStencils), false, enabledStencils)?.ApplyToObject();
            var slot = Slot.None;
            var parent = gameObject.Parent as GameObject;
            if (parent != null)
            {
                foreach (var tempSlot in parent.GetContainmentSlots())
                {
                    if (parent.GetContainedObject(tempSlot) == gameObject)
                    {
                        slot = tempSlot;
                        break;
                    }
                }
            }
            newGameObject.mPurchasedPrice = gameObject.mPurchasedPrice;
            newGameObject.mValueModifier = gameObject.mValueModifier;
            gameObject.Destroy();
            gameObject.Dispose();
            if (slot != Slot.None && !newGameObject.ParentToSlot(parent, slot))
            {
                GlobalFunctions.PlaceWithHandTool(newGameObject);
            }
            return true;
        }
        
        public static bool TryGetRandomItem<T>(this IEnumerable<T> enumerable, out T item) where T : class
        {
            var list = new List<T>(enumerable);
            return (item = list.Count == 0 ? null : list[RandomUtil.GetInt(list.Count - 1)]) != null;
        }

        public static bool TryGetRandomItem<T>(this IEnumerable<T> enumerable, out T? item) where T : struct
        {
            var list = new List<T>(enumerable);
            return (item = list.Count == 0 ? (T?)null : list[RandomUtil.GetInt(list.Count - 1)]).HasValue;
        }

        public static bool TryGetType(string fullName, out Type type)
        {
            type = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var tempType in assembly.GetTypes())
                {
                    if (tempType.FullName == fullName)
                    {
                        type = tempType;
                        return true;
                    }
                }
            }
            return false;
        }
        
        public static void Write(this IPropertyStreamWriter writer, object memberHolder, string memberName)
        {
            var field = memberHolder.GetType().GetField(memberName, (BindingFlags)0x34);
            var property = memberHolder.GetType().GetProperty(memberName, (BindingFlags)0x34);
            var value = field?.GetValue(memberHolder) ?? property?.GetValue(memberHolder, null);
            var nameHash = ResourceUtils.HashString32(memberName);
            switch ((field?.GetType() ?? property?.GetType())?.Name)
            {
                case nameof(Boolean):
                    writer.WriteBool(nameHash, Convert.ToBoolean(value));
                    break;
                case nameof(Byte):
                    writer.WriteByte(nameHash, Convert.ToByte(value));
                    break;
                case nameof(Int16):
                    writer.WriteInt16(nameHash, Convert.ToInt16(value));
                    break;
                case nameof(Int32):
                    writer.WriteInt32(nameHash, Convert.ToInt32(value));
                    break;
                case nameof(Int64):
                    writer.WriteInt64(nameHash, Convert.ToInt64(value));
                    break;
                case nameof(Single):
                    writer.WriteFloat(nameHash, Convert.ToSingle(value, CultureInfo.InvariantCulture));
                    break;
                case nameof(String):
                    writer.WriteString(nameHash, Convert.ToString(value));
                    break;
                case nameof(UInt16):
                    writer.WriteUint16(nameHash, Convert.ToUInt16(value));
                    break;
                case nameof(UInt32):
                    writer.WriteUint32(nameHash, Convert.ToUInt32(value));
                    break;
                case nameof(UInt64):
                    writer.WriteUint64(nameHash, Convert.ToUInt64(value));
                    break;
            }
        }
    }
}
