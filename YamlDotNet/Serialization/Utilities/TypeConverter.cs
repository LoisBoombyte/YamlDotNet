//  This file is part of YamlDotNet - A .NET library for YAML.
//  Copyright (c) Antoine Aubry and contributors

//  Permission is hereby granted, free of charge, to any person obtaining a copy of
//  this software and associated documentation files (the "Software"), to deal in
//  the Software without restriction, including without limitation the rights to
//  use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
//  of the Software, and to permit persons to whom the Software is furnished to do
//  so, subject to the following conditions:

//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.

//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.

// Remarks: This file is imported from the SixPack library. This is ok because
// the copyright holder has agreed to redistribute this file under the license
// used in YamlDotNet.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace YamlDotNet.Serialization.Utilities
{
    /// <summary>
    /// Performs type conversions using every standard provided by the .NET library.
    /// </summary>
    public static class TypeConverter
    {
#if !(NETSTANDARD1_3 || UNITY)
        /// <summary>
        /// Registers a <see cref="System.ComponentModel.TypeConverter"/> dynamically.
        /// </summary>
        /// <typeparam name="TConvertible">The type to which the converter should be associated.</typeparam>
        /// <typeparam name="TConverter">The type of the converter.</typeparam>
#endif
#if !(NETCOREAPP3_1 || NETCOREAPP3_0 || NETSTANDARD2_1 || NETSTANDARD1_3 || UNITY)
        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.LinkDemand, Name = "FullTrust")]
#endif
#if !(NETSTANDARD1_3 || UNITY)
        public static void RegisterTypeConverter<TConvertible, TConverter>()
            where TConverter : System.ComponentModel.TypeConverter
        {
            var alreadyRegistered = TypeDescriptor.GetAttributes(typeof(TConvertible))
                .OfType<TypeConverterAttribute>()
                .Any(a => a.ConverterTypeName == typeof(TConverter).AssemblyQualifiedName);

            if (!alreadyRegistered)
            {
                TypeDescriptor.AddAttributes(typeof(TConvertible), new TypeConverterAttribute(typeof(TConverter)));
            }
        }
#endif

        /// <summary>
        /// Converts the specified value.
        /// </summary>
        /// <typeparam name="T">The type to which the value is to be converted.</typeparam>
        /// <param name="value">The value to convert.</param>
        /// <returns></returns>
        public static T ChangeType<T>(object? value)
        {
            return (T)ChangeType(value, typeof(T))!; // This cast should always be valid
        }

        /// <summary>
        /// Converts the specified value.
        /// </summary>
        /// <typeparam name="T">The type to which the value is to be converted.</typeparam>
        /// <param name="value">The value to convert.</param>
        /// <param name="provider">The provider.</param>
        /// <returns></returns>
        public static T ChangeType<T>(object? value, IFormatProvider provider)
        {
            return (T)ChangeType(value, typeof(T), provider)!; // This cast should always be valid
        }

        /// <summary>
        /// Converts the specified value.
        /// </summary>
        /// <typeparam name="T">The type to which the value is to be converted.</typeparam>
        /// <param name="value">The value to convert.</param>
        /// <param name="culture">The culture.</param>
        /// <returns></returns>
        public static T ChangeType<T>(object? value, CultureInfo culture)
        {
            return (T)ChangeType(value, typeof(T), culture)!; // This cast should always be valid
        }

        /// <summary>
        /// Converts the specified value using the invariant culture.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="destinationType">The type to which the value is to be converted.</param>
        /// <returns></returns>
        public static object? ChangeType(object? value, Type destinationType)
        {
            return ChangeType(value, destinationType, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts the specified value.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="destinationType">The type to which the value is to be converted.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns></returns>
        public static object? ChangeType(object? value, Type destinationType, IFormatProvider provider)
        {
            return ChangeType(value, destinationType, new CultureInfoAdapter(CultureInfo.CurrentCulture, provider));
        }

        /// <summary>
        /// Converts the specified value.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="destinationType">The type to which the value is to be converted.</param>
        /// <param name="culture">The culture.</param>
        /// <returns></returns>
        public static object? ChangeType(object? value, Type destinationType, CultureInfo culture)
        {
            // Handle null and DBNull
            if (value == null || value.IsDbNull())
            {
                return destinationType.IsValueType() ? Activator.CreateInstance(destinationType) : null;
            }

            var sourceType = value.GetType();

            // If the source type is compatible with the destination type, no conversion is needed
            if (destinationType == sourceType || destinationType.IsAssignableFrom(sourceType))
            {
                return value;
            }

            // Nullable types get a special treatment
            if (destinationType.IsGenericType())
            {
                var genericTypeDefinition = destinationType.GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(Nullable<>))
                {
                    var innerType = destinationType.GetGenericArguments()[0];
                    var convertedValue = ChangeType(value, innerType, culture);
                    return Activator.CreateInstance(destinationType, convertedValue);
                }
            }

            // Enums also require special handling
            if (destinationType.IsEnum())
            {
                return value is string valueText
                    ? Enum.Parse(destinationType, valueText, true)
                    : value;
            }

            // Special case for booleans to support parsing "1" and "0". This is
            // necessary for compatibility with XML Schema.
            if (destinationType == typeof(bool))
            {
                if ("0".Equals(value))
                {
                    return false;
                }

                if ("1".Equals(value))
                {
                    return true;
                }
            }

            // Try with the source type's converter
            var sourceConverter = TypeDescriptor.GetConverter(sourceType);
            if (sourceConverter != null && sourceConverter.CanConvertTo(destinationType))
            {
                return sourceConverter.ConvertTo(null, culture, value, destinationType);
            }

            // Try with the destination type's converter
            var destinationConverter = TypeDescriptor.GetConverter(destinationType);
            if (destinationConverter != null && destinationConverter.CanConvertFrom(sourceType))
            {
                return destinationConverter.ConvertFrom(null, culture, value);
            }

            //For special Unity Serialization of int[], enum[], List<int>, List<enum>
            //we have a long string, composed of blocks of 8 chars, each of them
            //being 4 blocks of 2 hexa numbers. We have to read them from left to right.
            //Example : "000100000010000000200000f3650a00"
            // - 256   : "00010000"
            // - 4096  : "00100000"
            // - 8192  : "00200000"
            // - 681459: "f3650a00"
            if (sourceType == typeof(string))
            {
                //we want to catch int[], EnumType[], List<int>, List<EnumType>, EnumType
                if (destinationType.BaseType == typeof(Array) || string.Equals(destinationType.Name, "List`1"))
                {
                    //Each tab element (enum or int32 value) is serialized in a string of 8*char
                    string? strSource = value.ToString();

                    if (!string.IsNullOrEmpty(strSource))
                    {
                        //Deserialize data to a int[]
                        int[] dataTabInt = FromUnitySerializationToIntArray(strSource);

                        //Then we will test the different "destination Type cases"
                        //1. enum[]
                        //2. int[] (already done)
                        //3. List<enum>
                        //4. List<int>
                        if (destinationType.BaseType == typeof(Array))
                        {
                            //1. enum []
                            MethodInfo[] methods = destinationType.GetMethods();
                            if (methods.Length > 0 && methods[0].ReturnType.BaseType == typeof(System.Enum))
                            {
                                //find the wanted Enum Type
                                Type dataContentType = methods[0].ReturnType;

                                return FromIntArrayToEnumArray(dataContentType, dataTabInt);
                            }
                            //2. int[]
                            else
                            {
                                return dataTabInt;
                            }
                        }

                        if (string.Equals(destinationType.Name, "List`1"))
                        {
                            //3. List<enum>
                            MethodInfo[] methods = destinationType.GetMethods();
                            if (methods[3].ReturnType.BaseType == typeof(System.Enum))
                            {
                                //find the wanted Enum Type
                                Type dataContentType = methods[3].ReturnType;

                                return FromIntArrayToEnumList(destinationType, dataContentType, dataTabInt);
                            }
                            //4. List<int>
                            else
                            {
                                return FromIntArrayToIntList(dataTabInt);
                            }
                        }
                    }
                }
            }

            // Try to find a casting operator in the source or destination type
            foreach (var type in new[] { sourceType, destinationType })
            {
                foreach (var method in type.GetPublicStaticMethods())
                {
                    var isCastingOperator =
                        method.IsSpecialName &&
                        (method.Name == "op_Implicit" || method.Name == "op_Explicit") &&
                        destinationType.IsAssignableFrom(method.ReturnParameter.ParameterType);

                    if (isCastingOperator)
                    {
                        var parameters = method.GetParameters();

                        var isCompatible =
                            parameters.Length == 1 &&
                            parameters[0].ParameterType.IsAssignableFrom(sourceType);

                        if (isCompatible)
                        {
                            try
                            {
                                return method.Invoke(null, new[] { value });
                            }
                            catch (TargetInvocationException ex)
                            {
                                throw ex.Unwrap();
                            }
                        }
                    }
                }
            }

            // If source type is string, try to find a Parse or TryParse method
            if (sourceType == typeof(string))
            {
                try
                {
                    // Try with - public static T Parse(string, IFormatProvider)
                    var parseMethod = destinationType.GetPublicStaticMethod("Parse", typeof(string), typeof(IFormatProvider));
                    if (parseMethod != null)
                    {
                        return parseMethod.Invoke(null, new object[] { value, culture });
                    }

                    // Try with - public static T Parse(string)
                    parseMethod = destinationType.GetPublicStaticMethod("Parse", typeof(string));
                    if (parseMethod != null)
                    {
                        return parseMethod.Invoke(null, new object[] { value });
                    }
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.Unwrap();
                }
            }

            // Handle TimeSpan
            if (destinationType == typeof(TimeSpan))
            {
                return TimeSpan.Parse((string)ChangeType(value, typeof(string), CultureInfo.InvariantCulture)!);
            }

            // Default to the Convert class
            return Convert.ChangeType(value, destinationType, CultureInfo.InvariantCulture);
        }

        public static object? FromIntArrayToEnumArray(Type enumType, int[] dataArray)
        {
            //Create array's Type : enumType[]
            Type dataType = enumType.MakeArrayType();

            //Create its constructor
            Type[] constructorParameters = new Type[1] { typeof(int) };
            ConstructorInfo? constructorData = dataType.GetConstructor(constructorParameters);

            //then use constructor
            if (constructorData != null)
            {
                //we now get a new empty enumType[]{}
                object returnedData = constructorData.Invoke(new object[] { dataArray.Length });

                //Get the method to add elements to the Array
                MethodInfo? setValMethod = dataType.GetMethod("SetValue", new Type[2] { typeof(object), typeof(int) });

                if (setValMethod != null)
                {
                    for (int j = 0; j < dataArray.Length; j++)
                    {
                        //convert elements
                        object enumDataElem = Enum.Parse(enumType, dataArray[j].ToString());

                        //add elements to the array EnumType[]
                        setValMethod.Invoke(returnedData, new object[] { enumDataElem, j });
                    }
                    return returnedData;
                }
            }
            return null;
        }

        public static object? FromIntArrayToEnumList(Type listEnumType, Type enumType, int[] dataArray)
        {
            //Create its constructor
            Type[] constructorParameters = new Type[0];
            ConstructorInfo? constructorData = listEnumType.GetConstructor(constructorParameters);

            //then use constructor
            if (constructorData != null)
            {
                //get a new empty List<enumType>
                object returnedData = constructorData.Invoke(null);

                //Method to add elements to the List
                MethodInfo? addMethod = listEnumType.GetMethod("Add");

                if (addMethod != null)
                {
                    for (int j = 0; j < dataArray.Length; j++)
                    {
                        //convert elements
                        object enumDataElem = Enum.Parse(enumType, dataArray[j].ToString());

                        //add elements to the array EnumType[]
                        addMethod.Invoke(returnedData, new object[] { enumDataElem });
                    }
                    return returnedData;
                }
            }
            return null;
        }

        public static List<int> FromIntArrayToIntList(int[] dataArray)
        {
            int lenght = dataArray.Length;
            List<int> returnedList = new List<int>(lenght);

            for (int j = 0; j < lenght; j++)
            {
                returnedList.Add(dataArray[j]);
            }

            return returnedList;
        }

        //The string is composed of 4 blocks of 2 chars 
        //=> 4 blocks of a Hexadecimal value. But we need to read them from left
        //to right, instead of right to left for a normal Byte / Hex value
        // e.g : "f3650a00" ==> 681.459
        // "f3" => 243 * 2^0  = 243
        // "65" => 101 * 2^8  = 25.856
        // "0a" => 10  * 2^16 = 655.360
        // "00" => 0   * 2^24 = 0
        public static int[] FromUnitySerializationToIntArray(string source)
        {
            int totalStrLength = source.Length;
            int[] dataTabInt = new int[(totalStrLength) / 8];

            //" 8 char block " index
            int intBlockIndex = 0;

            //let's read 8 chars by 8 chars
            while (totalStrLength >= (intBlockIndex + 1) * 8)
            {
                string oneBlockStr = source.Substring(intBlockIndex * 8, 8);

                //Storing the int32 number
                int totalValue = 0;

                //read the 4 blocks of 2 chars, each block is a Hex number
                for (int i = 0; i < 4; i++)
                {
                    //first two chars of example : "f3"
                    string twoHexaChar = oneBlockStr.Substring(i * 2, 2);

                    //will thus store 243 
                    int.TryParse(twoHexaChar, NumberStyles.AllowHexSpecifier, new CultureInfo("en-US"), out int valueTwoHexaChar);

                    //then multiply : 243 * 2^0     and store result
                    totalValue += valueTwoHexaChar * (Convert.ToInt32(Math.Pow(2, i * 8)));
                }

                dataTabInt[intBlockIndex] = totalValue;
                intBlockIndex++;
            }

            return dataTabInt;
        }
    }
}
