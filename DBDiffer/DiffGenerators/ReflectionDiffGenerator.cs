using System;
using System.Collections.Generic;

namespace DBDiffer.DiffGenerators
{
    internal class ReflectionDiffGenerator
    {
        private readonly FieldMap _fieldMap;
        private readonly DBInfo _prevDB;
        private readonly DBInfo _curDB;

        public ReflectionDiffGenerator(DBInfo prevDB, DBInfo curDB)
        {
            _prevDB = prevDB;
            _curDB = curDB;
            _fieldMap = new FieldMap(prevDB.Fields.Keys, curDB.Fields.Keys);
        }

        public List<Diff> Generate(object a, object b)
        {
            if (a == null)
                throw new ArgumentNullException(nameof(a));
            if (b == null)
                throw new ArgumentNullException(nameof(b));

            return GetChanges(a, b);
        }


        private List<Diff> GetChanges(object a, object b)
        {
            var diffs = new List<Diff>(_fieldMap.Count);

            // same named fields are diffed
            foreach (var prop in _fieldMap.CommonFields)
                AppendChanges(a, b, prop, diffs);

            // new name fields are marked as added
            foreach (var prop in _fieldMap.AddedFields)
            {
                if (_curDB.Fields[prop].FieldType.IsArray)
                    AppendArrayChanges(_curDB.GetArrayFieldValue(b, prop), prop, DiffOperation.Added, diffs);
                else
                    diffs.Add(new Diff(DiffOperation.Added, prop, _curDB.GetFieldValue(b, prop)));
            }

            // old name fields are marked as removed
            foreach (var prop in _fieldMap.RemovedFields)
            {
                if (_prevDB.Fields[prop].FieldType.IsArray)
                    AppendArrayChanges(_prevDB.GetArrayFieldValue(a, prop), prop, DiffOperation.Removed, diffs);
                else
                    diffs.Add(new Diff(DiffOperation.Removed, prop, _prevDB.GetFieldValue(a, prop)));
            }

            return diffs;
        }

        private void AppendChanges(object a, object b, string path, List<Diff> diffs)
        {
            bool isArrayA = _prevDB.Fields[path].FieldType.IsArray;
            bool isArrayB = _curDB.Fields[path].FieldType.IsArray;

            if (isArrayA && isArrayB)
            {
                AppendArrayDiffChanges(a, b, path, diffs);
            }
            else if (isArrayA != isArrayB)
            {
                AppendArrayTypeChanges(a, b, isArrayA, path, diffs);
            }
            else
            {
                string valueA = _prevDB.GetFieldValue(a, path);
                string valueB = _curDB.GetFieldValue(b, path);

                if (!valueA.Equals(valueB))
                    diffs.Add(new Diff(DiffOperation.Replaced, path, valueB, valueA));
            }
        }


        /// <summary>
        /// Iterates two arrays comparing differences
        /// also catering for array resizes
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="path"></param>
        /// <param name="diffs"></param>
        private void AppendArrayDiffChanges(object a, object b, string path, List<Diff> diffs)
        {
            var a1 = _prevDB.GetArrayFieldValue(a, path);
            var a2 = _curDB.GetArrayFieldValue(b, path);

            int minLen = Math.Min(a1.Length, a2.Length);

            // direct compare of matching indicies
            string valueA, valueB;
            for (int i = 0; i < minLen; i++)
            {
                valueA = a1.GetValue(i).ToString();
                valueB = a2.GetValue(i).ToString();

                if (valueA != valueB)
                    diffs.Add(new Diff(DiffOperation.Replaced, $"{path}[{i}]", valueB, valueA));
            }

            // array was made smaller
            if (a1.Length > minLen)
                for (int i = minLen; i < a1.Length; i++)
                    diffs.Add(new Diff(DiffOperation.Removed, $"{path}[{i}]", a1.GetValue(i).ToString()));

            // array was made bigger
            if (a2.Length > minLen)
                for (int i = minLen; i < a2.Length; i++)
                    diffs.Add(new Diff(DiffOperation.Added, $"{path}[{i}]", a2.GetValue(i).ToString()));
        }

        /// <summary>
        /// For when a field switches between a field and an array.
        /// Marks the field as Added/Removed then marks the array as the inverse
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="arrayIdx"></param>
        /// <param name="path"></param>
        /// <param name="diffs"></param>
        private void AppendArrayTypeChanges(object a, object b, bool isArrayA, string path, List<Diff> diffs)
        {
            if (!isArrayA)
            {
                diffs.Add(new Diff(DiffOperation.Removed, path, _prevDB.GetFieldValue(a, path)));
                Array array = _curDB.GetArrayFieldValue(b, path);
                AppendArrayChanges(array, path, DiffOperation.Added, diffs);
            }
            else
            {
                diffs.Add(new Diff(DiffOperation.Added, path, _curDB.GetFieldValue(b, path)));
                Array array = _prevDB.GetArrayFieldValue(a, path);
                AppendArrayChanges(array, path, DiffOperation.Removed, diffs);
            }
        }

        /// <summary>
        /// Explodes the array and marks all indicies as the same operation
        /// </summary>
        /// <param name="array"></param>
        /// <param name="path"></param>
        /// <param name="op"></param>
        /// <param name="diffs"></param>
        private void AppendArrayChanges(Array array, string path, DiffOperation op, List<Diff> diffs)
        {
            for (int i = 0; i < array.Length; i++)
                diffs.Add(new Diff(op, $"{path}[{i}]", array.GetValue(i).ToString()));
        }
    }
}
