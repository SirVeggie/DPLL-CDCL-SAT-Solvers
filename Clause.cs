using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SAT_Solver {
    public class Clause : IEnumerable<Literal> {
        public List<Literal> Literals { get; }

        public int Count => Literals.Count;
        public Clause Reverse => new Clause(Literals.Select(x => x.Reverse).ToList());

        public Literal this[int key] {
            get => Literals[key];
            set => Literals[key] = value;
        }

        public Clause() {
            Literals = new List<Literal>();
        }

        public Clause(Clause clause) {
            Literals = new List<Literal>(clause.Literals);
        }

        public Clause(List<Literal> literals) {
            Literals = literals;
        }

        public void Add(Literal literal) {
            for (int i = 0; i < Count; i++) {
                if (Literals[i] == literal) {
                    if (Literals[i].value != literal.value)
                        Literals.RemoveAt(i);
                    return;
                }
            }

            Literals.Add(literal);
        }

        public bool Contains(Literal literal) => Literals.Contains(literal);
        public bool ContainsExact(Literal literal) {
            foreach (var lit in Literals) {
                if (lit.raw == literal.raw) {
                    return true;
                }
            }

            return false;
        }

        public void Resolution(Clause other) {
            int old = Count;
            foreach (var literal in other) {
                Add(literal);
            }

            if (old + other.Count == Count)
                throw new Exception("Resolution not possible for given clauses");
        }

        public bool Match(Clause clause) {
            if (Count != clause.Count)
                return false;
            foreach (var literal in clause) {
                if (!ContainsExact(literal)) {
                    return false;
                }
            }

            return true;
        }

        public override string ToString() => string.Join(" ", Literals.Select(x => x.ToString()));
        public IEnumerator<Literal> GetEnumerator() => Literals.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
