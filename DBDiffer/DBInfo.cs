using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using DBDiffer.Helpers;

namespace DBDiffer
{
    internal class DBInfo
    {
        public readonly IDictionary Storage;
        public readonly Dictionary<string, FieldInfo> Fields;

        private readonly Dictionary<string, FakeBinder> _binders;
        public DBInfo(IDictionary dictionary)
        {
            Storage = dictionary;
            var valueType = dictionary.GetType().GetGenericArguments().Last();
            Fields = valueType.GetFields().OrderBy(x => x.Name).ToDictionary(x => x.Name, x => x);

            _binders = Fields.ToDictionary(x => x.Key, x => new FakeBinder(x.Key));
        }

        public IEnumerable<int> Keys => Storage.Keys as ICollection<int>;


        public string GetFieldValue(object o, string prop)
        {
            if (o is DynamicObject dyn)
            {
                dyn.TryGetMember(_binders[prop], out var obj);
                return obj.ToString();
            }

            return Fields[prop].GetValue(o).ToString();
        }

        public Array GetArrayFieldValue(object o, string prop)
        {
            if (o is DynamicObject dyn)
            {
                dyn.TryGetMember(_binders[prop], out var obj);
                return obj as Array;
            }

            return Fields[prop].GetValue(o) as Array;
        }
    }
}
