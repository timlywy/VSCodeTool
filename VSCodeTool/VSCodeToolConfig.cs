using System;

namespace VSCodeTool
{
    [Serializable]
    public class VSCodeToolConfig
    {
        public bool AllowPolymorphism;
        public VSCodeToolJsonSchema[] JsonSchemas;
    }
    
    [Serializable]
    public class VSCodeToolJsonSchema
    {
        public string Class;
        public bool IsList;
        public string Path;
    }
    
    [AttributeUsage(AttributeTargets.Field)]
    public class VSCodeToolDescription : Attribute
    {
        public string Description;

        public VSCodeToolDescription(string description)
        {
            Description = description;
        }
    }
}
