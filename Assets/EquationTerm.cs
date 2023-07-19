using System.Linq;
using System;

namespace SetTheory
{
    public enum SetSymbol
    {
        Pacman,
        X,
        Triangle,
        Teepee,
        Shirt,
        Arrow,
        Diamond,
        H,
        Star
    }

    public enum SetOperation
    {
        Union,
        Intersection,
        Difference,
        SymmetricDifference,
        Complement
    }

    abstract class EquationTerm : object
    {
        public abstract SetSymbol[] Evaluate(SetSymbol[][] variables);
    }

    class EquationTermVariable : EquationTerm
    {
        public int VariableIndex { get; private set; }

        public EquationTermVariable(int index)
        {
            VariableIndex = index;
        }

        public override SetSymbol[] Evaluate(SetSymbol[][] variables)
        {
            return variables[VariableIndex];
        }

        public override string ToString()
        {
            return "ABC"[VariableIndex].ToString();
        }
    }

    class EquationTermOperation : EquationTerm
    {
        public SetOperation Operation { get; private set; }
        public EquationTerm Left { get; private set; }
        public EquationTerm Right { get; private set; }

        public EquationTermOperation(SetOperation op, EquationTerm left, EquationTerm right = null)
        {
            Operation = op;
            Left = left;
            Right = right;
        }

        private SetSymbol[] CalculateOperation(SetOperation op, SetSymbol[] a, SetSymbol[] b)
        {
            switch (op)
            {
                case SetOperation.Union:
                    return a.Union(b).ToArray();
                case SetOperation.Intersection:
                    return a.Intersect(b).ToArray();
                case SetOperation.Difference:
                    return a.Except(b).ToArray();
                case SetOperation.SymmetricDifference:
                    return ((SetSymbol[]) Enum.GetValues(typeof(SetSymbol))).Where(i => a.Contains(i) != b.Contains(i)).ToArray();
                case SetOperation.Complement:
                    return ((SetSymbol[]) Enum.GetValues(typeof(SetSymbol))).Where(i => !a.Contains(i)).ToArray();
                default:
                    throw new InvalidOperationException("Invalid set operation.");
            }
        }

        public override SetSymbol[] Evaluate(SetSymbol[][] variables)
        {
            return CalculateOperation(Operation, Left.Evaluate(variables), Right == null ? null : Right.Evaluate(variables));
        }

        public override string ToString()
        {
            string opSymbol;
            switch (Operation)
            {
                case SetOperation.Union: opSymbol = "∪"; break;
                case SetOperation.Intersection: opSymbol = "∩"; break;
                case SetOperation.Difference: opSymbol = "−"; break;
                case SetOperation.SymmetricDifference: opSymbol = "∆"; break;
                case SetOperation.Complement: return "!" + Left.ToString();
                default: throw new InvalidOperationException("Invalid set operation.");
            }
            return string.Format("({0} {1} {2})", Left, opSymbol, Right);
        }
    }
}
