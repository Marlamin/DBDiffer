using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DBDiffer.Helpers.Attributes;
using DBDiffer.Helpers.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DBDiffer.DiffResults
{
    [DiffType(DiffType.WoWTools)]
    public sealed class WoWToolsDiffResult : IDiffResult, IReadOnlyCollection<WoWToolsDiff>
    {
        private readonly List<WoWToolsDiff> _results;
        private readonly WoWToolsConverter _converter;

        internal WoWToolsDiffResult(DBInfo PreviousDB, DBInfo CurrentDB, IDictionary<int, List<Diff>> diffs)
        {
            _converter = new WoWToolsConverter();

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

        public void Save(string path, Formatting formatting = Formatting.None)
        {
            using (var fs = File.CreateText(path))
                fs.Write(JsonConvert.SerializeObject(this, formatting, _converter));
        }

        public string ToJSONString(Formatting formatting = Formatting.None)
        {
            var result = new { data = this };
            return JsonConvert.SerializeObject(result, formatting, _converter);
        }

        public string ToJSONString(int draw, int skip, int take, DiffOperation opFilter = DiffOperation.Unknown, Formatting formatting = Formatting.None)
        {
            var elements = this._results;

            if(opFilter != DiffOperation.Unknown)
                elements = elements.Where(x => x.Operation == opFilter).ToList();

            var eles = elements.Skip(Math.Max(skip, 0)).Take(take <= 0 ? Count : take);
            var result = new { draw = draw, data = eles, recordsTotal = this.Count, recordsFiltered = this.Count };

            return JsonConvert.SerializeObject(result, formatting, _converter);
        }

        #region Interface

        public int Count => _results.Count;

        public IEnumerator<WoWToolsDiff> GetEnumerator() => _results.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

    }

    public sealed class WoWToolsDiff
    {
        [JsonProperty("row")]
        public object Record { get; set; }
        [JsonProperty("op")]
        [JsonConverter(typeof(StringEnumConverter))]
        public DiffOperation Operation { get; set; }
        [JsonProperty("diff")]
        public List<Diff> Diffs { get; set; }
    }
}
