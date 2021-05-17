using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SAT_Solver {
    public static class Program {

        const string testfile = @"S:\- Library -\Projects\University\Automated logical reasoning\Simple Solver\test.cnf";
        const string satdir = @"S:\- Library -\Projects\University\Automated logical reasoning\SAT-Solver\sat-instances";

        static void Main(string[] args) {
            if (args[0] == "benchmark") {
                Benchmark(args[1], 120000);
                return;
            }

            string dimacs = File.ReadAllText(args[0]);
            string type = args.Length > 1 ? args[1] : "";
            string heuristic = args.Length > 2 ? args[2].ToLower() : null;
            bool debug = args.Length > 3 ? args[3].ToLower() == "debug" : false;

            var watch = Stopwatch.StartNew();
            SATSolver solver;
            if (type.ToLower() == "dpll")
                solver = new DPLL(dimacs, debug, heuristic);
            else if (type.ToLower() == "cdcl")
                solver = new CDCL(dimacs, debug, heuristic);
            else
                solver = new DPLL(dimacs, debug, heuristic);
            var result = solver.Run();
            Console.WriteLine("Time elapsed: " + watch.ElapsedMilliseconds + " ms");
            Console.WriteLine(result);
            CreateTestFile(dimacs, result);
        }

        static void Benchmark(string type, int timeout) {
            foreach (var file in Directory.GetFiles(satdir)) {
                Console.Write($"Solving file '{Path.GetFileName(file)}' with {(type.ToLower() == "cdcl" ? "CDCL" : "DPLL")}: ");
                var dimacs = File.ReadAllText(file);
                var watch = Stopwatch.StartNew();
                SATSolver solver;

                if (type.ToLower() == "cdcl") {
                    solver = new CDCL(dimacs, false, null, timeout);
                } else {
                    solver = new DPLL(dimacs, false, null, timeout);
                }

                var result = solver.Run();
                if (result.TimeoutTime > 0) {
                    Console.WriteLine($"Timed out at {result.TimeoutTime} ms");
                } else {
                    Console.WriteLine($"Found result {(result.Result ? "SATISFIABLE" : "UNSATISFIABLE")} in {watch.ElapsedMilliseconds} ms");
                }
            }
        }

        static void CreateTestFile(string dimacs, SolverResult result) {
            if (result.Result) {
                string clauses = Regex.Match(dimacs, @"p +cnf +\d+ +(\d+)").Groups[1].Value;
                dimacs = Regex.Replace(dimacs, @"(?<=p +cnf +\d+ +)\d+", int.Parse(clauses) + result.Variables.Count + "");
                dimacs += string.Join(' ', result.Variables.Select(x => $"\n{x.raw} 0"));
            }

            File.WriteAllText(testfile, dimacs);
        }
    }
}
