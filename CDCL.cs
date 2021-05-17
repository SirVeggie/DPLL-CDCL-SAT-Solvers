using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SAT_Solver {
    public class CDCL : SATSolver {

        /// <summary>Branch order is the predefined order in which literals are branched on. [index] is the order, [value] is the literal index.</summary>
        private List<Literal> BranchOrder;
        private Trail trail;
        private Conflict latestConflict;
        private string heuristic;
        private List<Clause> learnedClauses;

        private double[] vsids;
        private const double vsidsDecay = 0.01;

        public CDCL(string dimacs, bool debug, string heuristic, int timeout = 0) {
            this.debug = debug;
            this.heuristic = heuristic;
            this.timeout = timeout;
            ParseDimacs(dimacs);
            SetBranchOrder();
            Initialize();
        }

        private void SetBranchOrder() {
            BranchOrder = ClauseReferences.Select((clause, variable) => new { variable, clause }).OrderByDescending(x => x.clause.Count).Select(x => new Literal(x.variable, false)).ToList();

            if (heuristic == "reverse") {
                BranchOrder.Reverse();
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
            trail = new Trail();
            learnedClauses = new List<Clause>();
            vsids = new double[literals.Count];
        }

        public override SolverResult Run() {
            if (Running)
                throw new InvalidOperationException("Solver is already running");
            Running = true;
            totalwatch = Stopwatch.StartNew();

            InitialUnitPropagation(out Conflict conflict);

            if (conflict != null) {
                Running = false;
                return SolverResult.Fail(0);
            }

            if (AllAssigned) {
                Running = false;
                return SolverResult.Success(0, literals);
            }

            if (debug)
                Console.WriteLine();
            while (true) {
                if (debug && latestConflict != null) {
                    Console.WriteLine("- Conflict");
                    Console.WriteLine(trail.DebugMessage);
                }

                iterations++;
                TrailNode node = Travel();
                UnitPropagation(node, out conflict);

                if (debug) {
                    StateDebug();
                }

                if (conflict == null && AllAssigned) {
                    Running = false;
                    return SolverResult.Success(iterations, literals);
                }

                if (conflict != null && level == 0) {
                    Running = false;
                    return SolverResult.Fail(iterations);
                }

                if (conflict != null)
                    latestConflict = conflict;

                if (timeout != 0 && totalwatch.ElapsedMilliseconds > timeout) {
                    Running = false;
                    return SolverResult.Timeout(iterations, (int) totalwatch.ElapsedMilliseconds);
                }
            }
        }

        private void StateDebug() {
            Console.WriteLine($"i{iterations} L{level}: {string.Join(" ", trail.Where(n => n.IsDecision).Select(n => n.Literal.ToString()))}");
            Console.WriteLine($"Trail: {string.Join(" ", trail)}");
            if (learnedClauses.Count > 0)
                Console.WriteLine($"{string.Join("\n", learnedClauses.Select(x => x.ToString()))}");
            Console.WriteLine();
        }

        private void AssignLiteral(Literal literal) {
            if (literals[literal.index].HasValue)
                throw new ArgumentException("The given literal is already assigned");
            literals[literal.index] = literal;
            assignCount++;
        }

        private void UnassignLiteral(Literal literal) {
            if (!literals[literal.index].HasValue)
                throw new ArgumentException("The given literal is already unassigned");
            literals[literal.index] = null;
            assignCount--;
        }

        private TrailNode Travel() {
            if (latestConflict != null)
                return Backtrack();
            return Decision();
        }

        private TrailNode Decision(Literal? force = null) {
            level++;
            Literal literal = force ?? SelectLiteral();
            AssignLiteral(literal);
            var node = new TrailNode(level, literal);
            trail.Add(node);
            return node;
        }

        private TrailNode Backtrack() {
            var conflict = latestConflict;
            latestConflict = null;
            AddClause(conflict.Clause);
            RevertToLevel(conflict.Level);
            ApplyVSIDS(conflict.Clause);

            var empty = FindEmpty(conflict.Clause);

            if (debug) {
                Console.WriteLine($"Revert to level {level} | Learned: {conflict.Clause} | Empty: {string.Join(" ", empty)}");
                StateDebug();
            }

            if (empty.Count == 1) {
                AssignLiteral(empty[0]);
                var node = new TrailNode(level, empty[0], conflict.Clause);
                trail.Add(node);
                return node;
            } else {
                return Decision();
            }
        }

        private void RevertToLevel(int targetLevel) {
            for (int i = trail.Count - 1; i >= 0; i--) {
                if (trail[i].Level > targetLevel) {
                    UnassignLiteral(trail[i].Literal);
                    trail.Remove(i);
                }
            }

            level = targetLevel;
        }

        private List<Literal> FindEmpty(Clause clause) {
            List<Literal> list = new List<Literal>();
            foreach (var literal in clause) {
                if (!literals[literal.index].HasValue) {
                    list.Add(literal);
                }
            }

            return list;
        }

        private void ApplyVSIDS(Clause clause) {
            for (int i = 0; i < vsids.Length; i++) {
                vsids[i] *= 1 - vsidsDecay;
            }

            foreach (var lit in clause) {
                vsids[lit.index] += 1;
            }
        }

        private void InitialUnitPropagation(out Conflict conflict) {
            conflict = null;
            foreach (var clause in Clauses) {
                if (clause.Count == 1) {
                    Literal literal = clause[0];

                    if (!literals[literal.index].HasValue) {
                        AssignLiteral(literal);
                        var node = new TrailNode(level, literal, clause);
                        trail.Add(node);
                        UnitPropagation(node, out conflict);
                        if (conflict != null)
                            return;
                    }
                }
            }
        }

        private void UnitPropagation(TrailNode node, out Conflict conflict) {
            conflict = null;

            foreach (var clause in ClauseReferences[node.Literal.index]) {
                if (TryPropagate(node, clause, out bool error) is Literal empty) {
                    AssignLiteral(empty);
                    var newNode = new TrailNode(level, empty, clause);
                    trail.Add(newNode);
                    UnitPropagation(newNode, out conflict);
                    if (conflict != null)
                        return;
                }

                if (error) {
                    var learnedClause = trail.GetUIP(clause, out int targetLevel);
                    conflict = new Conflict(targetLevel, learnedClause);
                    return;
                }
            }
        }

        private Literal? TryPropagate(TrailNode node, Clause clause, out bool conflict) {
            int assignedValues = 0;
            int falseValues = 0;
            Literal empty = default;
            conflict = false;

            foreach (var lit in clause) {
                if (literals[lit.index].HasValue) {
                    if (literals[lit.index].Value.value != lit.value)
                        falseValues++;
                    assignedValues++;
                } else {
                    empty = lit;
                }
            }

            if (assignedValues != falseValues) {
                return null;
            }

            if (assignedValues == clause.Count - 1) {
                return empty;
            }

            if (falseValues == clause.Count) {
                conflict = true;
                return null;
            }

            return null;
        }

        private void AddClause(Clause clause) {
            learnedClauses.Add(clause);
            Clauses.Add(clause);
            foreach (var literal in clause) {
                ClauseReferences[literal.index].Add(clause);
            }
        }

        private Literal SelectLiteral() {
            if (heuristic == "random")
                return RandomPick();
            if (heuristic == "vsids")
                return VSIDS();
            return UseBranchOrder();
        }

        #region heuristics
        private Literal UseBranchOrder() {
            for (int i = 0; i < LiteralCount; i++) {
                var index = BranchOrder[i].index;
                if (!literals[index].HasValue) {
                    return new Literal(index, false);
                }
            }

            throw new Exception("Branchable literal not found");
        }

        private Literal RandomPick() {
            var rnd = new Random();

            while (true) {
                var val = rnd.Next(LiteralCount);

                if (!literals[val].HasValue) {
                    return new Literal(val, false);
                }
            }
        }

        private Literal VSIDS() {
            foreach (var item in vsids.Select((value, index) => new { index, value }).OrderByDescending(x => x.value)) {
                if (!literals[item.index].HasValue) {
                    return new Literal(item.index, false);
                }
            }

            throw new Exception("Branchable literal not found");
        }
        #endregion
    }
}
