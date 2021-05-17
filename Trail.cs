using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SAT_Solver {
    public class Trail : IEnumerable<TrailNode> {

        private List<TrailNode> list = new List<TrailNode>();
        private Dictionary<int, TrailNode> map = new Dictionary<int, TrailNode>();

        public string DebugMessage { get; private set; }
        public int Count => list.Count;

        public TrailNode this[int key] {
            get => list[key];
            set => list[key] = value;
        }

        public void Add(TrailNode node) {
            list.Add(node);
            map.Add(node.Literal.index, node);
        }

        public void Remove(int index) {
            var node = list[index];
            list.RemoveAt(index);
            map.Remove(node.Literal.index);
        }

        public TrailNode Find(Literal literal) => map.ContainsKey(literal.index) ? map[literal.index] : null;

        public TrailNode FindLatestMatch(Clause clause) {
            var set = clause.ToHashSet();
            for (int i = list.Count - 1; i >= 0; i--) {
                if (set.Contains(this[i].Literal)) {
                    return this[i];
                }
            }

            throw new Exception("Match not found");
        }

        public Clause GetUIP(Clause source, out int level) {
            DebugMessage = $"Conflict nodes: {string.Join(" ", source.Select(x => Find(x)))}";

            var clause = new Clause(source);
            int currentLevel = list[Count - 1].Level;

            for (int i = Count - 1; i >= 0; i--) {
                if (list[i].IsDecision)
                    break;
                if (!HasMultipleTops(clause))
                    break;
                if (clause.Contains(list[i].Literal)) {
                    clause.Resolution(list[i].Clause);
                }
            }

            level = FindBacktrackLevel(clause);
            return clause;
        }

        public bool HasMultipleTops(Clause clause) {
            int count = 0;
            foreach (var literal in clause) {
                if (map[literal.index].Level == list[Count - 1].Level) {
                    if (count > 0)
                        return true;
                    count++;
                }
            }

            return false;
        }

        public int FindBacktrackLevel(Clause clause) {
            int highest = 0;
            int second = 0;

            foreach (var literal in clause) {
                var node = map[literal.index];
                if (node.Level > highest) {
                    second = highest;
                    highest = node.Level;
                } else if (node.Level > second && node.Level != highest) {
                    second = node.Level;
                }
            }

            return second;
        }

        public IEnumerator<TrailNode> GetEnumerator() => list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
