using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using DBDiffer.Helpers.Converters;

namespace DBDiffer.DiffResults
{
    public sealed class WoWToolsDiffResult : IReadOnlyCollection<WoWToolsDiff>
    {
        private readonly List<WoWToolsDiff> _results;

        internal WoWToolsDiffResult(DBInfo PreviousDB, DBInfo CurrentDB, IDictionary<int, List<Diff>> diffs)
        {
            var intersection = PreviousDB.Keys.Intersect(CurrentDB.Keys);

            var added = CurrentDB.Keys.Except(intersection).Select(x => new WoWToolsDiff
            {
                Record = CurrentDB.Storage[x],
                Operation = DiffOperation.Added
            });

            var removed = PreviousDB.Keys.Except(intersection).Select(x => new WoWToolsDiff
            {
                Record = PreviousDB.Storage[x],
                Operation = DiffOperation.Removed
            });

            var modified = diffs.Select(x => new WoWToolsDiff
            {
                Diffs = x.Value,
                Operation = DiffOperation.Replaced,
                Record = PreviousDB.Storage[x.Key]
            });

            _results = added.Concat(removed).Concat(modified).ToList();
        }

        public void Save(string path, bool indented = false)
        {
            using (var fs = File.CreateText(path))
                fs.Write(JsonSerializer.Serialize(this, JsonSerialization.CreateOptions(indented, new WoWToolsConverter())));
        }

        public string ToJSONString(bool indented = false)
        {
            var result = new { data = this };
            return JsonSerializer.Serialize(result, JsonSerialization.CreateOptions(indented, new WoWToolsConverter()));
        }

        public string ToJSONString(int draw, int skip, int take, DiffOperation opFilter = DiffOperation.Unknown, bool indented = false)
        {
            var elements = this._results;

            if (opFilter != DiffOperation.Unknown)
                elements = elements.Where(x => x.Operation == opFilter).ToList();

            var eles = elements.Skip(Math.Max(skip, 0)).Take(take <= 0 ? Count : take);
            var result = new { draw = draw, data = eles, recordsTotal = this.Count, recordsFiltered = this.Count };

            return JsonSerializer.Serialize(result, JsonSerialization.CreateOptions(indented, new WoWToolsConverter()));
        }

        #region Interface

        public int Count => _results.Count;

        public IEnumerator<WoWToolsDiff> GetEnumerator() => _results.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

    }

    public sealed class WoWToolsDiff
    {
        [JsonPropertyName("row")]
        public object Record { get; set; }
        [JsonPropertyName("op")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DiffOperation Operation { get; set; }
        [JsonPropertyName("diff")]
        public List<Diff> Diffs { get; set; } = [];
    }
}
