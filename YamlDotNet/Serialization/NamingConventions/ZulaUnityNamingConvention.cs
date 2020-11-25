using YamlDotNet.Serialization.Utilities;

namespace YamlDotNet.Serialization.NamingConventions
{
    //So Unity serializes a ScriptableObject with : 
    //private Type _field;
    //public Type Field{get ... set ...}
    // in --->  _field: content
    //So we needed this special Naming Convention
    public class ZulaUnityNamingConvention : INamingConvention
    {
        private ZulaUnityNamingConvention() { }

        public string Apply(string value)
        {
            return "_" + value.ToCamelCase();
        }

        public static readonly INamingConvention Instance = new ZulaUnityNamingConvention();
    }
}