using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
using System;
using System.ComponentModel;
using System.Text;
using System.IO;

public static class CSVParser
{
    private static List<FieldInfo> fileFieldInfoList = new List<FieldInfo>();
    private static List<string> objFieldNameList = new List<string>();
    private static Type stringType = typeof(string);
    private static Dictionary<Type, TypeConverter> converterDictionary = new Dictionary<Type, TypeConverter>();

    #region CONSTANT
    private const char COMMA = ',';
    private const char LINE_FEED1 = '\n';
    private const char LINE_FEED2 = '\r';
    private const char DOUBLE_QUOTE = '"';
    private const string EMPTY_STR = "";
    #endregion

    public static List<T> Convert<T>(string source) where T : new()
    {
        // ★ Final Result.
        List<T> resultList = new List<T>();

        // extracts the field -
        // skips all headrs and sets the cursor after the header. 
        int cursor = ParseHeaderByFile<T>(source);

        // vars for extracting the content.
        Type fieldType = null;
        FieldInfo fieldInfo = null;
        int columnCount = 0;
        T data = default(T);
        bool isDoubleQuote = false;
        char prevChar = LINE_FEED1;
        string targetValue = "";

        // Extract Process
        for (int i = cursor; i < source.Length; i++)
        {
            char thisChar = source[i];

            switch (thisChar)
            {
                case LINE_FEED1:
                case LINE_FEED2:

                    // ignores during line feed.
                    if (prevChar == LINE_FEED1 || prevChar == LINE_FEED2)
                        break;

                    // Adds the value if remaining column exists.
                    if (columnCount < fileFieldInfoList.Count)
                    {
                        // ---- STEP 1. Get targetValue
                        // Create new data if no column exists
                        if (columnCount == 0)
                            data = new T();

                        // Adds in the column.
                        fieldInfo = fileFieldInfoList[columnCount];

                        // if the content is empty, add empty string
                        if (i - cursor <= 1)
                            targetValue = EMPTY_STR;
                        else
                            targetValue = source.Substring(cursor + 1, i - cursor - 1);

                        // Additional parsing process if double quote exists.
                        if (targetValue.IndexOf(DOUBLE_QUOTE) > -1)
                            targetValue = LoadParse(targetValue);

                        // Extra parsing process for targetValue
                        targetValue = ExParse(targetValue);

                        // ---- STEP 2. Get the data
                        // don't need to add anything if the string is empty.
                        if (!string.IsNullOrEmpty(targetValue))
                        {
                            // Attachs the value to property.    
                            fieldType = fileFieldInfoList[columnCount].FieldType;
                            if (fieldType.Equals(stringType))
                            {
                                // string type needs boxing. boxes only on class type, skips boxing on struct type.
                                object boxedData = data;
                                fieldInfo.SetValue(boxedData, targetValue);
                                data = (T)boxedData;
                            }
                            else
                            {
                                if (!converterDictionary.ContainsKey(fieldType))
                                    converterDictionary.Add(fieldType, TypeDescriptor.GetConverter(fieldType));

                                // Add column to the target T object.
                                fieldInfo.SetValue(data, converterDictionary[fieldType].ConvertFrom(targetValue));
                            }
                        }
                    }

                    // Add the extracted data.
                    resultList.Add(data);

                    // Deinit all.
                    cursor = i;
                    columnCount = 0;
                    isDoubleQuote = false;

                    break;
                case DOUBLE_QUOTE:
                    // Everything between double quotes should be treated as string.
                    isDoubleQuote = !isDoubleQuote;

                    break;
                case COMMA:
                    // ignores between double quotes.
                    if (isDoubleQuote)
                        break;

                    // ignores if no more columns left.
                    if (columnCount >= fileFieldInfoList.Count)
                        break;

                    // ---- STEP 1. Get targetValue
                    // Create new data if no column exists
                    if (columnCount == 0)
                        data = new T();

                    // Adds in the column.
                    fieldInfo = fileFieldInfoList[columnCount];

                    // if the content is empty, add empty string
                    if (i - cursor <= 1)
                        targetValue = EMPTY_STR;
                    else
                        targetValue = source.Substring(cursor + 1, i - cursor - 1);

                    // Do additional parsing process if double quote exists.
                    if (targetValue.IndexOf(DOUBLE_QUOTE) > -1)
                        targetValue = LoadParse(targetValue);

                    // Extra parsing process for targetValue
                    targetValue = ExParse(targetValue);

                    // ---- STEP 2. Get the data
                    // don't need to add anything if the string is empty.
                    if (!string.IsNullOrEmpty(targetValue))
                    {
                        // Attachs the value to property.
                        fieldType = fileFieldInfoList[columnCount].FieldType;

                        if (fieldType.Equals(stringType))
                        {
                            // string type needs boxing. boxes only on class type, skips boxing on struct type.
                            object boxedData = data;
                            fieldInfo.SetValue(boxedData, targetValue);
                            data = (T)boxedData;
                        }
                        else
                        {
                            if (!converterDictionary.ContainsKey(fieldType))
                                converterDictionary.Add(fieldType, TypeDescriptor.GetConverter(fieldType));

                            // Add column to the target T object.
                            fieldInfo.SetValue(data, converterDictionary[fieldType].ConvertFrom(targetValue));
                        }
                    }

                    cursor = i;
                    columnCount++;
                    break;
                default:
                    // ignores all else.
                    break;
            }

            prevChar = thisChar;
        }

        return resultList;
    }

    // Extra Parsing Process.
    private static string ExParse(string targetValue)
    {
        // first character should not to be LINE FEED.
        if (targetValue.IndexOf(LINE_FEED1) == 0 || targetValue.IndexOf(LINE_FEED2) == 0)
            targetValue = targetValue.Replace("\n", "").Replace("\r", "");

        // '\n' string should not to be inserted for raw.
        if (targetValue.IndexOf("\\n") > -1)
            targetValue = targetValue.Replace("\\n", "\n");

        return targetValue;
    }

    // Extracts the header of the file.
    private static int ParseHeaderByFile<T>(string source)
    {
        // cursor position of the file.
        int cursor = -1;

        // Extracts the header of the target object.
        FieldInfo[] objProperties = typeof(T).GetFields();
        ParseHeaderByObj(objProperties);

        // Extracts the header of the file.
        fileFieldInfoList.Clear();
        for (int i = 0; i < source.Length; i++)
        {
            char oneChar = source[i];

            if (oneChar == COMMA || oneChar == LINE_FEED1 || oneChar == LINE_FEED2)
            {
                // if any contents exist, save in the header.
                if (i > cursor + 1)
                {
                    string targetFieldName = source.Substring(cursor + 1, i - cursor - 1);

                    // other method to find the column position corresponds to the field name.
                    //int targetIndex = mObjFieldNameList.FindIndex((string x) => { return x.Equals(targetFieldName, StringComparison.InvariantCultureIgnoreCase); });
                    int targetIndex = objFieldNameList.IndexOf(targetFieldName);
                    fileFieldInfoList.Add(targetIndex < 0 ? null : objProperties[targetIndex]);
                }

                cursor = i;
                if (oneChar == LINE_FEED1 || oneChar == LINE_FEED2)
                    break;
            }
        }
        return cursor;
    }

    // Extracts the header of the object.
    private static void ParseHeaderByObj(FieldInfo[] objProperties)
    {
        objFieldNameList.Clear();
        for (int i = 0; i < objProperties.Length; i++)
            objFieldNameList.Add(objProperties[i].Name);
    }

    // Parses the string on objectizing.
    private static string LoadParse(string target)
    {
        if (target.IndexOf("\"\"") > -1)
            target = target.Replace("\"\"", "#@quote#");
        target = target.Replace("\"", "");
        target = target.Replace("#@quote#", "\"");

        if (target.IndexOf(",") > -1 && target.IndexOf('"') == 0 && target.LastIndexOf('"') == 0)
            target = target.Substring(1, target.Length - 2);

        return target;
    }
}