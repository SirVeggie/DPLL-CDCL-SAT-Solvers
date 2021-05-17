using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SAT_Solver {
    public class SolverResult {

        public bool Result { get; }
        public int TimeoutTime { get; }
        public int Iterations { get; }
        public List<Literal> Variables { get; }

        public SolverResult(bool result, int iterations, List<Literal> variables, int timeout = 0) {
            Result = result;
            Variables = variables;
            TimeoutTime = timeout;
            Iterations = iterations;
        }

        public static SolverResult Fail(int iterations) => new SolverResult(false, iterations, new List<Literal>(), 0);
        public static SolverResult Success(int iterations, List<Literal?> variables) => new SolverResult(true, iterations, (variables ?? new List<Literal?>()).Select(x => x ?? throw new Exception("Cannot give unassigned variables to the result")).ToList(), 0);
        public static SolverResult Timeout(int iterations, int timeout) => new SolverResult(false, iterations, new List<Literal>(), timeout);

        public override string ToString() {
            string result = $"Iterations: {Iterations}\n";
            if (TimeoutTime > 0)
                result += $"Solver timed out at {TimeoutTime} ms\ns UNSATISFIABLE";
            else
                result += !Result ? "s UNSATISFIABLE" : "s SATISFIABLE\nv " + string.Join(' ', Variables.Select(x => x.ToString())) + " 0";
            return result;
        }
    }
}
