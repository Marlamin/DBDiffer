using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DBDiffer.DiffGenerators;
using DBDiffer.DiffResults;

namespace DBDiffer
{
    public class DBComparer
    {
        private readonly DBInfo PreviousDB;
        private readonly DBInfo CurrentDB;

        public DBComparer(IDictionary previousdb, IDictionary currentdb)
        {
            PreviousDB = new DBInfo(previousdb);
            CurrentDB = new DBInfo(currentdb);
        }

        public WoWToolsDiffResult Diff()
        {
            var diffs = ObjectDiff(PreviousDB.Keys.Intersect(CurrentDB.Keys));
            return new WoWToolsDiffResult(PreviousDB, CurrentDB, diffs);
        }

        private IDictionary<int, List<Diff>> ObjectDiff(IEnumerable<int> keys)
        {
            var differ = new ReflectionDiffGenerator(PreviousDB, CurrentDB);

            var resultset = new Dictionary<int, List<Diff>>(keys.Count());
            Parallel.ForEach(keys, key =>
            {
                var diffs = differ.Generate(PreviousDB.Storage[key], CurrentDB.Storage[key]);
                if (diffs.Count == 0)
                    return;

                lock (resultset)
                    resultset.Add(key, diffs);
            });

            return resultset;
        }
    }
}
