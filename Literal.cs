using System;
using System.Collections.Generic;
using System.Text;

namespace SAT_Solver {
    public struct Literal {
        public readonly int index;
        public readonly bool value;
        public readonly int raw;

        public Literal Reverse => new Literal(index, !value);

        public Literal(int literal) {
            raw = literal;
            index = Math.Abs(literal) - 1;
            value = literal > 0;
        }

        public Literal(int index, bool value) {
            this.index = index;
            this.value = value;
            raw = value ? index + 1 : -(index + 1);
        }

        public static bool operator ==(Literal a, Literal b) => a.index == b.index;
        public static bool operator !=(Literal a, Literal b) => !(a == b);
        public override bool Equals(object obj) => obj is Literal literal && literal == this;
        public override int GetHashCode() => HashCode.Combine(index);
        public override string ToString() => raw.ToString();
    }
}
