using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace VSCodeTool
{
    public static class VSCodeTool
    {
        private static readonly string _toolFolderPath = Application.dataPath.Replace
            ("Assets", "VSCodeTool");
        private static readonly string _workSpacePath = Application.dataPath.Replace
            ("Assets", $"/{Application.productName}.code-workspace");
        private static readonly string _toolConfigPath = Application.dataPath.Replace
            ("Assets", "VSCodeTool") + "/ToolConfig.json";
        
        [MenuItem("VSCodeTool/InitConfig")]
        public static void InitConfig()
        {
            if (EditorUtility.DisplayDialog("提示", "初始化会清除所有Config，是否继续？", "是", "否"))
            {
                if (Directory.Exists(_toolFolderPath))
                {
                    //Delete Files
                    string[] files = Directory.GetFiles(_toolFolderPath);
                    foreach (string file in files)
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                    }
                }
                else
                {
                    //Create Folder
                    Directory.CreateDirectory(_toolFolderPath);
                }
                //Create WorkSpace Config
                using (StreamWriter workSpaceWriter = new StreamWriter(_workSpacePath))
                {
                    workSpaceWriter.WriteLine("{");
                    workSpaceWriter.WriteLine("\t\"folders\":");
                    workSpaceWriter.WriteLine("\t[");
                    workSpaceWriter.WriteLine("\t\t{\"path\": \"./VSCodeTool\"}");
                    workSpaceWriter.WriteLine("\t],");
                    workSpaceWriter.WriteLine("\t\"settings\":");
                    workSpaceWriter.WriteLine("\t{");
                    workSpaceWriter.WriteLine("\t\t\"files.exclude\": {\"**/*.meta\": true}");
                    workSpaceWriter.WriteLine("\t}");
                    workSpaceWriter.WriteLine("}");
                }
                //Create ToolConfig
                using (StreamWriter toolConfigWriter = new StreamWriter(_toolConfigPath))
                {
                    toolConfigWriter.WriteLine("{");
                    toolConfigWriter.WriteLine("\t\"AllowPolymorphism\": true,");
                    toolConfigWriter.WriteLine("\t\"JsonSchemas\":");
                    toolConfigWriter.WriteLine("\t[");
                    toolConfigWriter.WriteLine("\t\t{");
                    toolConfigWriter.WriteLine("\t\t\t\"Class\": \"NameSpace.Class\",");
                    toolConfigWriter.WriteLine("\t\t\t\"IsList\": false,");
                    toolConfigWriter.WriteLine("\t\t\t\"Path\": \"Assets/.../Folder\"");
                    toolConfigWriter.WriteLine("\t\t}");
                    toolConfigWriter.WriteLine("\t]");
                    toolConfigWriter.WriteLine("}");
                }
                EditorUtility.DisplayDialog("提示", "初始化已完成", "是");
            }
        }

        [MenuItem("VSCodeTool/UpdateSchema")]
        public static void UpdateSchema()
        {
            ClearAllData();
            GetToolConfig();
            if (_vsCodeToolConfig != null
                && _vsCodeToolConfig.JsonSchemas != null
                && _vsCodeToolConfig.JsonSchemas.Length > 0)
            {
                WriteToDefinitions();
                WriteToJsonSchemas();
                WriteToWorkSpace();
                EditorUtility.DisplayDialog("提示", "Schema更新已完成", "是");
            }
        }

        private static VSCodeToolConfig _vsCodeToolConfig;
        private static Queue<string> _definitionTodoList = new Queue<string>();
        private static List<string> _definitionDoneList = new List<string>();
        private static List<string> _curSubClassList = new List<string>();
        
        private static void ClearAllData()
        {
            _vsCodeToolConfig = null;
            _definitionTodoList.Clear();
            _definitionDoneList.Clear();
            _curSubClassList.Clear();
        }
        
        private static void GetToolConfig()
        {
            string toolConfigStr = File.ReadAllText(_toolConfigPath);
            _vsCodeToolConfig = JsonUtility.FromJson<VSCodeToolConfig>(toolConfigStr);
        }

        private static void WriteToDefinitions()
        {
            string definitionsPath = _toolFolderPath + "/Definitions.json";
            using (StreamWriter definitionsWriter = new StreamWriter(definitionsPath))
            {
                definitionsWriter.WriteLine("{");
                definitionsWriter.WriteLine("\t\"title\": \"Definitions\",");
                definitionsWriter.WriteLine("\t\"definitions\":");
                definitionsWriter.WriteLine("\t{");
                for (int i = 0; i < _vsCodeToolConfig.JsonSchemas.Length; i++)
                {
                    if (!_definitionTodoList.Contains(_vsCodeToolConfig.JsonSchemas[i].Class)
                        && !_definitionDoneList.Contains(_vsCodeToolConfig.JsonSchemas[i].Class))
                    {
                        _definitionTodoList.Enqueue(_vsCodeToolConfig.JsonSchemas[i].Class);
                    }
                }
                while (_definitionTodoList.Count > 0)
                {
                    string toWriteDefinition = _definitionTodoList.Dequeue();
                    WriteDefinition(toWriteDefinition, definitionsWriter);
                    _definitionDoneList.Add(toWriteDefinition);
                }
                definitionsWriter.WriteLine("\t}");
                definitionsWriter.WriteLine("}");
            }
        }

        private static void WriteToJsonSchemas()
        {
            for (int i = 0; i < _vsCodeToolConfig.JsonSchemas.Length; i++)
            {
                string schemaPath = _toolFolderPath + $"/{_vsCodeToolConfig.JsonSchemas[i].Class}_Schema.json";
                using (StreamWriter schemaWriter = new StreamWriter(schemaPath))
                {
                    schemaWriter.WriteLine("{");
                    schemaWriter.WriteLine($"\t\"title\": \"{_vsCodeToolConfig.JsonSchemas[i].Class}_Schema\",");
                    if (_vsCodeToolConfig.JsonSchemas[i].IsList)
                    {
                        schemaWriter.WriteLine("\t\"type\": \"array\",");
                        schemaWriter.WriteLine("\t\"items\": {\"$ref\": \"Definitions.json#/definitions/" +
                                               _vsCodeToolConfig.JsonSchemas[i].Class + "\"}");
                    }
                    else
                    {
                        schemaWriter.WriteLine("\t\"$ref\": \"Definitions.json#/definitions/" +
                                               $"{_vsCodeToolConfig.JsonSchemas[i].Class}\"");
                    }
                    schemaWriter.WriteLine("}");
                }
            }
        }

        private static void WriteToWorkSpace()
        {
            using (StreamWriter workSpaceWriter = new StreamWriter(_workSpacePath))
            {
                workSpaceWriter.WriteLine("{");
                workSpaceWriter.WriteLine("\t\"folders\":");
                workSpaceWriter.WriteLine("\t[");
                workSpaceWriter.WriteLine("\t\t{\"path\": \"./VSCodeTool\"},");
                for (int i = 0; i < _vsCodeToolConfig.JsonSchemas.Length; i++)
                {
                    workSpaceWriter.WriteLine("\t\t{\"path\": \"./"
                                              + _vsCodeToolConfig.JsonSchemas[i].Path + "\"},");
                }
                workSpaceWriter.WriteLine("\t],");
                workSpaceWriter.WriteLine("\t\"settings\":");
                workSpaceWriter.WriteLine("\t{");
                workSpaceWriter.WriteLine("\t\t\"files.exclude\": {\"**/*.meta\": true},");
                workSpaceWriter.WriteLine("\t\t\"json.schemas\":");
                workSpaceWriter.WriteLine("\t\t[");
                for (int i = 0; i < _vsCodeToolConfig.JsonSchemas.Length; i++)
                {
                    workSpaceWriter.WriteLine("\t\t\t{");
                    workSpaceWriter.WriteLine($"\t\t\t\t\"url\": \"./VSCodeTool/" +
                                              $"{_vsCodeToolConfig.JsonSchemas[i].Class}_Schema.json\",");
                    workSpaceWriter.WriteLine($"\t\t\t\t\"fileMatch\": [\"/" +
                                              $"{_vsCodeToolConfig.JsonSchemas[i].Path}/**.json\"]");
                    workSpaceWriter.WriteLine("\t\t\t},");
                }
                workSpaceWriter.WriteLine("\t\t]");
                workSpaceWriter.WriteLine("\t}");
                workSpaceWriter.WriteLine("}");
            }
        }

        private static void WriteDefinition(string className, StreamWriter writer)
        {
            Type definitionType = Type.GetType(className);
            if (definitionType == null || writer == null)
            {
                return;
            }
            writer.WriteLine($"\t\t\"{className}\": " + "{\"type\": \"object\", \"properties\": {");
            var fieldInfos = definitionType.GetFields(BindingFlags.Instance | BindingFlags.Public);
            if (fieldInfos.Length > 0)
            {
                for (int i = 0; i < fieldInfos.Length; i++)
                {
                    Type fieldType = fieldInfos[i].FieldType;
                    if (CheckProperty(fieldType))
                    {
                        if (i < fieldInfos.Length - 1)
                        {
                            writer.WriteLine($"\t\t\t\"{fieldInfos[i].Name}\": " + "{" + GetProperty(fieldType)
                                             + GetDescription(fieldInfos[i]) + "},");
                        }
                        else
                        {
                            writer.WriteLine($"\t\t\t\"{fieldInfos[i].Name}\": " + "{" + GetProperty(fieldType)
                                             + GetDescription(fieldInfos[i]) + "}");
                        }
                    }
                }
            }
            _curSubClassList = GetSubClass(className);
            if (_vsCodeToolConfig.AllowPolymorphism && _curSubClassList.Count > 0)
            {
                writer.WriteLine("\t\t}},");
                writer.WriteLine($"\t\t\"{className}[Polymorphism]\": " + "{\"type\": \"object\", \"allOf\": " +
                                 "[{\"properties\": {\"$type\": {\"enum\": [");
                for (int i = 0; i < _curSubClassList.Count; i++)
                {
                    if (i < _curSubClassList.Count - 1)
                    {
                        writer.WriteLine($"\t\t\t\"{_curSubClassList[i]}\",");
                    }
                    else
                    {
                        writer.WriteLine($"\t\t\t\"{_curSubClassList[i]}\"");
                    }
                }
                writer.WriteLine("\t\t\t]}}},");
                for (int i = 0; i < _curSubClassList.Count; i++)
                {
                    writer.WriteLine("\t\t\t{\"if\": {\"properties\": {\"$type\": {" +
                                     $"\"const\": \"{_curSubClassList[i]}\"" +
                                     "}}, \"minProperties\": 2},");
                    writer.WriteLine("\t\t\t\"then\": {\"properties\": {");
                    Type subDefinitionType = Type.GetType(_curSubClassList[i]);
                    if (subDefinitionType != null)
                    {
                        var subFieldInfos = subDefinitionType.GetFields
                            (BindingFlags.Instance | BindingFlags.Public);
                        if (subFieldInfos.Length > 0)
                        {
                            for (int j = 0; j < subFieldInfos.Length; j++)
                            {
                                Type subFieldType = subFieldInfos[j].FieldType;
                                if (CheckProperty(subFieldType))
                                {
                                    if (j < subFieldInfos.Length - 1)
                                    {
                                        writer.WriteLine($"\t\t\t\t\"{subFieldInfos[j].Name}\": " + "{"
                                            + GetProperty(subFieldType) + GetDescription(subFieldInfos[j]) + "},");
                                    }
                                    else
                                    {
                                        writer.WriteLine($"\t\t\t\t\"{subFieldInfos[j].Name}\": " + "{"
                                            + GetProperty(subFieldType) + GetDescription(subFieldInfos[j]) + "}");
                                    }
                                }
                            }    
                        }
                    }

                    if (i < _curSubClassList.Count - 1)
                    {
                        writer.WriteLine("\t\t\t}}},");
                    }
                    else
                    {
                        writer.WriteLine("\t\t\t}}}");
                    }
                }
                
                if (_definitionTodoList.Count > 0)
                {
                    writer.WriteLine("\t\t]},");
                }
                else
                {
                    writer.WriteLine("\t\t]}");
                }
            }
            else
            {
                if (_definitionTodoList.Count > 0)
                {
                    writer.WriteLine("\t\t}},");
                }
                else
                {
                    writer.WriteLine("\t\t}}");
                }
            }
        }

        private static bool CheckProperty(Type propertyType)
        {
            if (propertyType == null)
            {
                return false;
            }
            if (propertyType.IsArray)
            {
                return CheckProperty(propertyType.GetElementType());
            }
            if (propertyType.IsGenericType
                && propertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return CheckProperty(propertyType.GetGenericArguments()[0]);
            }
            if (propertyType.IsGenericType
                && propertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>)
                && propertyType.GetGenericArguments()[0] == typeof(string))
            {
                return CheckProperty(propertyType.GetGenericArguments()[1]);
            }
            return propertyType == typeof(float) || propertyType == typeof(bool) || propertyType.IsClass
                   || propertyType == typeof(int) || (propertyType.IsValueType && !propertyType.IsPrimitive)
                   || propertyType == typeof(string);
        }
        
        private static string GetProperty(Type propertyType)
        {
            if (propertyType.IsArray)
            {
                return "\"type\": \"array\", \"items\": {" + GetProperty(propertyType.GetElementType()) + "}";
            }
            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type genericType = propertyType.GetGenericArguments()[0];
                if (genericType.IsClass && _vsCodeToolConfig.AllowPolymorphism
                                        && GetSubClass(genericType.FullName).Count > 0)
                {
                    if (!_definitionTodoList.Contains(genericType.FullName)
                        && !_definitionDoneList.Contains(genericType.FullName))
                    {
                        _definitionTodoList.Enqueue(genericType.FullName);
                    }
                    return "\"type\": \"array\", \"items\": {" + 
                           $"\"$ref\": \"#/definitions/{genericType.FullName}[Polymorphism]\"" + "}";
                }
                return "\"type\": \"array\", \"items\": {" + GetProperty(genericType) + "}";
            }
            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>) 
                                           && propertyType.GetGenericArguments()[0] == typeof(string))
            {
                Type genericType = propertyType.GetGenericArguments()[1];
                if (genericType.IsClass && _vsCodeToolConfig.AllowPolymorphism
                                        && GetSubClass(genericType.FullName).Count > 0)
                {
                    if (!_definitionTodoList.Contains(genericType.FullName)
                        && !_definitionDoneList.Contains(genericType.FullName))
                    {
                        _definitionTodoList.Enqueue(genericType.FullName);
                    }
                    return "\"type\": \"object\", \"additionalProperties\": {" + 
                           $"\"$ref\": \"#/definitions/{genericType.FullName}[Polymorphism]\"" + "}";
                }
                return "\"type\": \"object\", \"additionalProperties\": {" + GetProperty(genericType) + "}";
            }
            if (propertyType == typeof(float))
            {
                return "\"type\": \"number\"";
            }
            if (propertyType == typeof(bool))
            {
                return "\"type\": \"boolean\"";
            }
            if (propertyType == typeof(string))
            {
                return "\"type\": \"string\"";
            }
            if (propertyType.IsClass || (propertyType.IsValueType && !propertyType.IsPrimitive && !propertyType.IsEnum))
            {
                if (!_definitionTodoList.Contains(propertyType.FullName)
                    &&!_definitionDoneList.Contains(propertyType.FullName))
                {
                    _definitionTodoList.Enqueue(propertyType.FullName);
                }
                return $"\"$ref\": \"#/definitions/{propertyType.FullName}\"";
            }
            if (propertyType.IsEnum)
            {
                string[] options = Enum.GetNames(propertyType);
                string propertyStr = "\"enum\": [";
                for (int i = 0; i < options.Length; i++)
                {
                    if (i < options.Length - 1)
                    {
                        propertyStr += $"\"{options[i]}\",";
                    }
                    else
                    {
                        propertyStr += $"\"{options[i]}\"";
                    }
                }
                propertyStr += "]";
                return propertyStr;
            }
            if (propertyType == typeof(int))
            {
                return "\"type\": \"integer\"";
            }
            return "";
        }

        private static string GetDescription(FieldInfo info)
        {
            if (info != null)
            {
                Attribute attribute = info.GetCustomAttribute(typeof(VSCodeToolDescription));
                if (attribute != null && attribute is VSCodeToolDescription)
                {
                    var descriptionAttribute = attribute as VSCodeToolDescription;
                    return $", \"description\": \"{descriptionAttribute.Description}\"";
                }
            }
            return "";
        }
        
        private static List<string> GetSubClass(string baseName)
        {
            Type baseType = Type.GetType(baseName);
            List<string> subClass = new List<string>();
            if (baseType != null)
            {
                var assembly = Assembly.GetAssembly(baseType);
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (type.IsSubclassOf(baseType) && !type.IsAbstract)
                    {
                        subClass.Add(type.FullName);
                    }
                }
            }
            return subClass;
        }
    }
}
