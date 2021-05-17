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
            if (args.Length == 0) {
                Console.WriteLine("No arguments given");
                return;
            }

            if (args[0] == "benchmark") {
                if (args.Length < 3) {
                    Console.WriteLine("Not enough arguments");
                    return;
                }

                Benchmark(args.Length < 4 ? null : args[3], args[1], args[2], 120000);
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

            if (new DirectoryInfo(testfile).Exists) {
                CreateTestFile(dimacs, result);
            }
        }

        static void Benchmark(string folder, string type, string heuristic, int timeout) {
            var dir = new DirectoryInfo(folder ?? satdir);
            if (!dir.Exists) {
                Console.WriteLine("Couldn't find specified folder");
            }

            foreach (var file in dir.GetFiles()) {
                if (file.Extension != ".cnf") {
                    continue;
                }

                Console.Write($"Solving file '{file.Name}' with {(type.ToLower() == "cdcl" ? "CDCL" : "DPLL")}: ");
                var dimacs = File.ReadAllText(file.FullName);
                var watch = Stopwatch.StartNew();
                SATSolver solver;

                if (type.ToLower() == "cdcl") {
                    solver = new CDCL(dimacs, false, heuristic, timeout);
                } else {
                    solver = new DPLL(dimacs, false, heuristic, timeout);
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
