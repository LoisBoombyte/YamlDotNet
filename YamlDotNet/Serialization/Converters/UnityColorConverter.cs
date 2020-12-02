using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace YamlDotNet.Serialization
{
    public class Color
    {
        private float _r;
        private float _g;
        private float _b;
        private float _a;

        public float r
        {
            get
            {
                return _r;
            }
            set
            {
                _r = value;
            }
        }
        public float g
        {
            get
            {
                return _g;
            }
            set
            {
                _g = value;
            }
        }
        public float b
        {
            get
            {
                return _b;
            }
            set
            {
                _b = value;
            }
        }
        public float a
        {
            get
            {
                return _a;
            }
            set
            {
                _a = value;
            }
        }
    }
}

namespace YamlDotNet.Serialization.Converters
{
    /// <summary>
    /// Example with Unity serialization of Color : _laserColor: {r: 1, g: 0, b: 1, a: 1}
    /// Let's convert it in "class Color", simple class with 4 float fields.
    /// </summary>
    public class UnityColorConverter : IYamlTypeConverter
    {
        public UnityColorConverter()
        {
        }

        public bool Accepts(Type type)
        {
            return string.Equals(type.FullName, "YamlDotNet.Serialization.Color");
        }

        public object ReadYaml(IParser parser, Type type)
        {
            parser.MoveNext();
            parser.MoveNext();
            var rValue = parser.Consume<Scalar>().Value;
            parser.MoveNext();
            var gValue = parser.Consume<Scalar>().Value;
            parser.MoveNext();
            var bValue = parser.Consume<Scalar>().Value;
            parser.MoveNext();
            var aValue = parser.Consume<Scalar>().Value;
            parser.MoveNext();

            Color returnColor = new Color()
            {
                r = float.Parse(rValue),
                g = float.Parse(gValue),
                b = float.Parse(bValue),
                a = float.Parse(aValue)
            };

            return returnColor;
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