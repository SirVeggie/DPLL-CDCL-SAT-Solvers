using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SAT_Solver {
    public class Conflict {
        public int Level { get; }
        public Clause Clause { get; }

        public Conflict(int level, Clause clause) {
            Level = level;
            Clause = clause;
        }
    }
}
