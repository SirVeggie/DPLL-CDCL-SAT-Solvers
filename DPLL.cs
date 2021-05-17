using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SAT_Solver {
    public class DPLL : SATSolver {

        /// <summary>Branch order is the predefined order in which literals are branched on. [index] is the order, [value] is the literal index.</summary>
        private List<Literal> BranchOrder;
        /// <summary>List of literals which were propagated at [index] decision level</summary>
        protected List<HashSet<Literal>> propagates;
        private List<bool> Defaults;
        private readonly string heuristic;
        private bool backtrack;
        private int leftBranches;

        public DPLL(string dimacs, bool debug, string heuristic = null, int timeout = 0) {
            this.debug = debug;
            this.heuristic = heuristic;
            this.timeout = timeout;
            ParseDimacs(dimacs);
            SetBranchInfo();
            Initialize();
        }

        private void SetBranchInfo() {
            BranchOrder = ClauseReferences.Select((clause, variable) => new { variable, clause }).OrderByDescending(x => x.clause.Count).Select(x => new Literal(x.variable, false)).ToList();

            if (heuristic == "reverse") {
                BranchOrder.Reverse();
            }

            Defaults = new List<bool>();
            for (int i = 0; i < LiteralCount; i++) {
                int T = 0;
                int F = 0;
                foreach (var clause in ClauseReferences[i]) {
                    foreach (var literal in clause) {
                        if (literal.index == i) {
                            if (literal.value)
                                T++;
                            else
                                F++;
                        }
                    }
                }

                if (T >= F)
                    Defaults.Add(true);
                else
                    Defaults.Add(false);
            }

            if (debug) {
                for (int i = 0; i < BranchOrder.Count; i++) {
                    var lit = BranchOrder[i];
                    Console.WriteLine($"{i}: {lit.index + 1}\t\t{lit.index + 1}'s clauses: {ClauseReferences[lit.index].Count}");
                }
            }
        }

        private void Initialize() {
            level = 0;
            branch = new Stack<Literal>();
            propagates = new List<HashSet<Literal>>();
            for (int i = 0; i < LiteralCount; i++)
                propagates.Add(new HashSet<Literal>());
        }

        public override SolverResult Run() {
            if (Running)
                throw new InvalidOperationException("Solver is already running");
            Running = true;
            totalwatch = Stopwatch.StartNew();

            InitialUnitPropagation(out bool conflict);

            if (conflict) {
                Running = false;
                return SolverResult.Fail(iterations);
            }

            if (AllAssigned) {
                Running = false;
                return SolverResult.Success(iterations, literals);
            }

            while (true) {
                iterations++;
                Literal variable = Branch();
                UnitPropagation(variable, out conflict);

                if (debug) {
                    Console.WriteLine($"branching, level: {level} tree: {string.Join(" ", branch.Select(x => x.ToString()).Reverse())}");
                    Console.WriteLine("propagations: " + string.Join(" ", propagates.SelectMany(x => x)));
                    Console.WriteLine("literals: " + string.Join(" ", literals.Select((x, i) => i + ":" + (x == null ? "__" : x.ToString()))));
                    Console.WriteLine();
                }

                if (!conflict && AllAssigned) {
                    Running = false;
                    return SolverResult.Success(iterations, literals);
                }

                if (conflict && leftBranches == 0) {
                    Running = false;
                    return SolverResult.Fail(iterations);
                }

                if (conflict)
                    backtrack = true;

                if (timeout != 0 && totalwatch.ElapsedMilliseconds > timeout) {
                    totalwatch.Stop();
                    return SolverResult.Timeout(iterations, (int) totalwatch.ElapsedMilliseconds);
                }
            }
        }

        private Literal Branch() {
            if (backtrack)
                return Backtrack();
            return BranchLeft();
        }

        private Literal Backtrack() {
            while (true) {
                if (debug)
                    Console.WriteLine("Backtrack");
                backtrack = false;

                foreach (var literal in propagates[level]) {
                    literals[literal.index] = null;
                    assignCount--;
                }

                propagates[level] = new HashSet<Literal>();

                Literal old = branch.Peek();
                literals[old.index] = null;
                assignCount--;

                if (old.value == Defaults[old.index]) {
                    return BranchRight();
                }

                level--;
                branch.Pop();
            }
        }

        private Literal BranchLeft() {
            if (debug)
                Console.WriteLine("Branch left");
            var literal = SelectLiteral();
            branch.Push(literal);

            level++;
            leftBranches++;
            literals[literal.index] = literal;
            assignCount++;
            return literal;
        }

        private Literal BranchRight() {
            if (debug)
                Console.WriteLine("Branch right");
            leftBranches--;
            var old = branch.Pop();
            var literal = new Literal(old.index, !Defaults[old.index]);
            branch.Push(literal);
            literals[literal.index] = literal;
            assignCount++;
            return literal;
        }

        protected void InitialUnitPropagation(out bool conflict) {
            foreach (var clause in Clauses) {
                if (clause.Count == 1) {
                    UnitPropagation(clause[0], out conflict);
                    if (conflict)
                        return;
                }
            }

            //Console.WriteLine("initial propagations: " + string.Join(" ", propagates.SelectMany(x => x)));

            conflict = false;
        }

        protected void UnitPropagation(Literal variable, out bool conflict) {
            conflict = false;
            foreach (var clause in ClauseReferences[variable.index]) {
                TryPropagate(clause, out conflict);

                if (conflict) {
                    return;
                }
            }

            return;
        }

        protected bool TryPropagate(Clause clause, out bool conflict) {
            int assignedValues = 0;
            int falseValues = 0;
            Literal literal = default;
            conflict = false;

            foreach (var lit in clause) {
                if (literals[lit.index].HasValue) {
                    if (literals[lit.index].Value.value != lit.value)
                        falseValues++;
                    assignedValues++;
                } else {
                    literal = lit;
                }
            }

            if (assignedValues != falseValues) {
                return false;
            }

            if (assignedValues == clause.Count - 1) {
                propagates[level].Add(literal);
                literals[literal.index] = literal;
                assignCount++;
                UnitPropagation(literal, out conflict);
                return true;
            }

            if (falseValues == clause.Count) {
                conflict = true;
                return false;
            }

            return false;
        }

        private Literal SelectLiteral() {
            if (heuristic == "random")
                return RandomPick();
            return UseBranchOrder();
        }

        #region heuristics
        private Literal UseBranchOrder() {
            for (int i = 0; i < LiteralCount; i++) {
                var index = BranchOrder[i].index;
                if (!literals[index].HasValue) {
                    return new Literal(index, Defaults[index]);
                }
            }

            throw new Exception("Branchable literal not found");
        }

        private Literal RandomPick() {
            var rnd = new Random();

            while (true) {
                var val = rnd.Next(LiteralCount);

                if (!literals[val].HasValue) {
                    return new Literal(val, Defaults[val]);
                }
            }
        }
        #endregion
    }
}
