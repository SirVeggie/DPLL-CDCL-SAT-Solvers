using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SAT_Solver {
    public abstract class SATSolver {

        public int LiteralCount { get; protected set; }
        public int ClauseCount { get; protected set; }
        /// <summary>List of clauses where a <see cref="List{T}"/> is a clause</summary>
        public List<Clause> Clauses { get; protected set; }
        /// <summary>List of references where index is a literal's index (n:th literal - 1) and the value is a list of clause indexes where that literal appears in (clause index is zero based)</summary>
        public List<List<Clause>> ClauseReferences { get; protected set; }
        public bool Running { get; protected set; }
        public bool AllAssigned => assignCount == LiteralCount;

        protected bool debug;
        protected int level;
        protected int assignCount;
        protected List<Literal?> literals;
        protected Stack<Literal> branch;
        protected int iterations = 0;
        protected Stopwatch totalwatch;
        protected int timeout;

        public abstract SolverResult Run();

        protected void ParseDimacs(string dimacs) {
            StringBuilder builder = new StringBuilder();
            foreach (var line in dimacs.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)) {
                if (line.First() == 'c')
                    continue;
                builder.Append(line + " ");
            }

            string[] items = builder.ToString().Split(' ', 5, StringSplitOptions.RemoveEmptyEntries);
            if (items[0] != "p")
                throw new ArgumentException("Expected p at start of dimacs (ignoring comments)");
            if (items[1] != "cnf")
                throw new ArgumentException("Expected cnf");
            LiteralCount = int.Parse(items[2]);
            ClauseCount = int.Parse(items[3]);

            string[] clauses = items[4].Split(" 0 ", StringSplitOptions.RemoveEmptyEntries);

            Clauses = new List<Clause>();
            ClauseReferences = new List<List<Clause>>();
            literals = new List<Literal?>();

            for (int i = 0; i < LiteralCount; i++) {
                ClauseReferences.Add(new List<Clause>());
                literals.Add(null);
            }

            for (int i = 0; i < clauses.Length; i++) {
                string strclause = clauses[i];
                List<Literal> variables = strclause.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(x => new Literal(int.Parse(x))).ToList();
                var clause = new Clause(variables);
                Clauses.Add(clause);
                foreach (var variable in variables) {
                    try {
                        ClauseReferences[variable.index].Add(clause);
                    } catch (IndexOutOfRangeException) {
                        throw new ArgumentException("Contains a variable higher than number of variables");
                    }
                }
            }

            if (ClauseCount != Clauses.Count)
                throw new ArgumentException($"Invalid number of clauses, given:{ClauseCount} found:{Clauses.Count}");
        }
    }
}
