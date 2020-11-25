using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace YamlDotNet.Serialization.Converters
{
    /// <summary>
    /// The goal is to Deserialize a boolean with a value of 0 or 1 instead of true/false
    /// </summary>
    public class UnityBooleanConverter : IYamlTypeConverter
    {
        public UnityBooleanConverter()
        {
        }

        public bool Accepts(Type type)
        {
            return type == typeof(Boolean);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            var value = parser.Consume<Scalar>().Value;

            int intVal;
            if (int.TryParse(value, out intVal))
            {
                return intVal == 0 ? false : true;
            }

            return new object();
        }

        /// <summary>
        /// TODO / not necessary, we just wanna read the Unity SO's file :D
        /// </summary>
        public void WriteYaml(IEmitter emitter, object? value, Type type)
        {
            //var dt = (DateTime)value!;
            //var adjusted = this.kind == DateTimeKind.Local ? dt.ToLocalTime() : dt.ToUniversalTime();
            //var formatted = adjusted.ToString(this.formats.First(), this.provider); // Always take the first format of the list.

            //emitter.Emit(new Scalar(null, null, formatted, ScalarStyle.Any, true, false));
        }
    }
}