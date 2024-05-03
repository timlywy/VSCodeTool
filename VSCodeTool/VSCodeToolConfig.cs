using System;

namespace VSCodeTool
{
    public class VSCodeToolConfig
    {
        public bool AllowPolymorphism;
        public VSCodeToolJsonSchema[] JsonSchemas;
    }
    
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