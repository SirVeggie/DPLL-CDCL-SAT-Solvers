using System;
using System.Collections.Generic;
using System.Text;

namespace SAT_Solver {
    public class TrailNode {
        public int Level { get; }
        public Literal Literal { get; }
        public Clause Clause { get; }
        public bool IsDecision => Clause == null;

        public TrailNode(int level, Literal literal, Clause clause = null) {
            Level = level;
            Literal = literal;
            Clause = clause;
        }

        public override string ToString() => $"{{{Level}: {Literal}}}";
    }
}
