using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Data;
using System.Diagnostics.Metrics;
using System.Reflection.Metadata;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FaxToTaxLibrary
{
    public class Calc
    {
        private class mcSymbol : IComparer
        {
            public string Token { get; set; }
            public Calc.TOKENCLASS Cls { get; set; }
            public PRECEDENCE PrecedenceLevel { get; set; }
            public string tag { get; set; }

            public delegate int compare_function(object x, object y);

            public int Compare(object x, object y)
            {
                mcSymbol asym = (mcSymbol)x;
                mcSymbol bsym = (mcSymbol)y;

                if (asym.Token.CompareTo(bsym.Token) > 0) return 1;
                if (asym.Token.CompareTo(bsym.Token) < 0) return -1;
                if (asym.PrecedenceLevel == PRECEDENCE.NONE || bsym.PrecedenceLevel == PRECEDENCE.NONE) return 0;
                if (asym.PrecedenceLevel > bsym.PrecedenceLevel) return 1;
                if (asym.PrecedenceLevel < bsym.PrecedenceLevel) return -1;

                return 0;
            }
        }

        private enum PRECEDENCE
        {
            NONE = 0,
            LEVEL0 = 1,
            LEVEL1 = 2,
            LEVEL2 = 3,
            LEVEL3 = 4,
            LEVEL4 = 5,
            LEVEL5 = 6
        }

        private enum TOKENCLASS
        {
            KEYWORD = 1,
            IDENTIFIER = 2,
            NUMBER = 3,
            OPERATR = 4,
            PUNCTUATION = 5
        }

        private string[] m_KeyWords;
        private const string ALPHA = "_ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string DIGITS = "#0123456789";

        private static void init_operators(ref ArrayList m_operators)
        {
            mcSymbol op;

            m_operators = new ArrayList();

            op = new mcSymbol { Token = "-", Cls = TOKENCLASS.OPERATR, PrecedenceLevel = PRECEDENCE.LEVEL1 };
            m_operators.Add(op);

            op = new mcSymbol { Token = "+", Cls = TOKENCLASS.OPERATR, PrecedenceLevel = PRECEDENCE.LEVEL1 };
            m_operators.Add(op);

            op = new mcSymbol { Token = "*", Cls = TOKENCLASS.OPERATR, PrecedenceLevel = PRECEDENCE.LEVEL2 };
            m_operators.Add(op);

            op = new mcSymbol { Token = "/", Cls = TOKENCLASS.OPERATR, PrecedenceLevel = PRECEDENCE.LEVEL2 };
            m_operators.Add(op);

            op = new mcSymbol { Token = "\\", Cls = TOKENCLASS.OPERATR, PrecedenceLevel = PRECEDENCE.LEVEL2 };
            m_operators.Add(op);

            op = new mcSymbol { Token = "%", Cls = TOKENCLASS.OPERATR, PrecedenceLevel = PRECEDENCE.LEVEL2 };
            m_operators.Add(op);

            op = new mcSymbol { Token = "!", Cls = TOKENCLASS.OPERATR, PrecedenceLevel = PRECEDENCE.LEVEL5 };
            m_operators.Add(op);

            op = new mcSymbol { Token = "&", Cls = TOKENCLASS.OPERATR, PrecedenceLevel = PRECEDENCE.LEVEL5 };
            m_operators.Add(op);

            op = new mcSymbol { Token = "-", Cls = TOKENCLASS.OPERATR, PrecedenceLevel = PRECEDENCE.LEVEL4 };
            m_operators.Add(op);

            op = new mcSymbol { Token = "+", Cls = TOKENCLASS.OPERATR, PrecedenceLevel = PRECEDENCE.LEVEL4 };
            m_operators.Add(op);

            op = new mcSymbol { Token = "(", Cls = TOKENCLASS.OPERATR, PrecedenceLevel = PRECEDENCE.LEVEL5 };
            m_operators.Add(op);

            op = new mcSymbol { Token = ")", Cls = TOKENCLASS.OPERATR, PrecedenceLevel = PRECEDENCE.LEVEL0 };
            m_operators.Add(op);

            op = new mcSymbol { Token = "=", Cls = TOKENCLASS.OPERATR, PrecedenceLevel = PRECEDENCE.LEVEL1 };
            m_operators.Add(op);

            op = new mcSymbol { Token = ">", Cls = TOKENCLASS.OPERATR, PrecedenceLevel = PRECEDENCE.LEVEL1 };
            m_operators.Add(op);

            op = new mcSymbol { Token = "<", Cls = TOKENCLASS.OPERATR, PrecedenceLevel = PRECEDENCE.LEVEL1 };
            m_operators.Add(op);

            op = new mcSymbol { Token = "#", Cls = TOKENCLASS.OPERATR, PrecedenceLevel = PRECEDENCE.LEVEL1 };
            m_operators.Add(op);

            op = new mcSymbol { Token = "^", Cls = TOKENCLASS.OPERATR, PrecedenceLevel = PRECEDENCE.LEVEL1 };
            m_operators.Add(op);

            op = new mcSymbol { Token = "$", Cls = TOKENCLASS.OPERATR, PrecedenceLevel = PRECEDENCE.LEVEL1 };
            m_operators.Add(op);

            m_operators.Sort(op);
        }

        public static double Evaluate(string expression)
        {
            Queue symbols = null;

            int[,] m_State = null;
            string m_colstring = "";
            ArrayList m_operators = null  ;
            string[] m_funcs = { "sin", "cos", "tan", "arcsin", "arccos", "arctan", "sqrt", "max", "min", "floor", "ceiling", "log", "log10", "ln", "round", "abs", "neg", "pos" };

            Init(ref m_State, ref m_colstring, m_funcs, ref m_operators);

            if (double.TryParse(expression, out double result)) return result;

            Calc_scan(expression, ref symbols, ref m_State, ref m_colstring, m_funcs);

            return level0(symbols, m_operators);
        }
        private static void Init(ref int[,] m_State, ref string m_colstring, string[] m_funcs, ref ArrayList m_operators)
        {
           
            int[,] state = {
        {2, 4, 1, 1, 4, 6, 7},
        {2, 3, 3, 3, 3, 3, 3},
        {1, 1, 1, 1, 1, 1, 1},
        {2, 4, 5, 5, 4, 5, 5},
        {1, 1, 1, 1, 1, 1, 1},
        {1, 1, 1, 1, 1, 1, 1},
        {1, 1, 1, 1, 1, 1, 1}
    };

            init_operators(ref m_operators);

            m_State = state;
            m_colstring = "\t .()";
            foreach (mcSymbol op in m_operators)
            {
                m_colstring += op.Token;
            }

            Array.Sort(m_funcs);
            // m_tokens = new Collection();
        }

        private static void Calc_scan(string line, ref Queue symbols, ref int[,] m_State, ref string m_colstring, string[] m_funcs)
        {
            // Implement the scanning logic here
        }

        private static double level0(Queue symbols, ArrayList m_operators)
        {
            // Implement the evaluation logic here
            return 0.0;
        }
        private static double Calc_op(mcSymbol op, double operand1, double operand2 = 0)
        {
            switch (op.Token.ToLower())
            {
                case "&": // sample to show addition of custom operator
                    return 5;

                case "+":
                    switch (op.PrecedenceLevel)
                    {
                        case PRECEDENCE.LEVEL1:
                            return operand2 + operand1;
                        case PRECEDENCE.LEVEL4:
                            return operand1;
                    }
                    break;

                case "-":
                    switch (op.PrecedenceLevel)
                    {
                        case PRECEDENCE.LEVEL1:
                            return operand1 - operand2;
                        case PRECEDENCE.LEVEL4:
                            return -1 * operand1;
                    }
                    break;

                case "*":
                    return operand2 * operand1;

                case "/":
                    return operand1 / operand2;

                case "\\":
                    return (long)operand1 / (long)operand2;

                case "%":
                    return operand1 % operand2;

                case "!":
                    double res = 1;
                    if (operand1 > 1)
                    {
                        for (int i = (int)operand1; i > 1; i--)
                        {
                            res *= i;
                        }
                    }
                    return res;

                case "=":
                    return operand1 == operand2 ? 1 : 0;

                case ">":
                    return operand1 > operand2 ? 1 : 0;

                case "<":
                    return operand1 < operand2 ? 1 : 0;

                case "#":
                    return operand1 != operand2 ? 1 : 0;

                case "^":
                    return operand1 >= operand2 ? 1 : 0;

                case "$":
                    return operand1 <= operand2 ? 1 : 0;
            }

            return 0;
        }

        private static double calc_function(string func, Collection args)
        {
            switch (func.ToLower())
            {
                case "cos":
                    return Math.Cos(Convert.ToDouble(args[1]));

                case "sin":
                    return Math.Sin(Convert.ToDouble(args[1]));

                case "tan":
                    return Math.Tan(Convert.ToDouble(args[1]));

                case "floor":
                    return Math.Floor(Convert.ToDouble(args[1]));

                case "ceiling":
                    return Math.Ceiling(Convert.ToDouble(args[1]));

                case "max":
                    return Math.Max(Convert.ToDouble(args[1]), Convert.ToDouble(args[2]));

                case "min":
                    return Math.Min(Convert.ToDouble(args[1]), Convert.ToDouble(args[2]));

                case "arcsin":
                    return Math.Asin(Convert.ToDouble(args[1]));

                case "arccos":
                    return Math.Acos(Convert.ToDouble(args[1]));

                case "arctan":
                    return Math.Atan(Convert.ToDouble(args[1]));

                case "sqrt":
                    return Math.Sqrt(Convert.ToDouble(args[1]));

                case "log":
                    return Math.Log10(Convert.ToDouble(args[1]));

                case "log10":
                    return Math.Log10(Convert.ToDouble(args[1]));

                case "abs":
                    return Math.Abs(Convert.ToDouble(args[1]));

                case "round":
                    return Math.Round(Convert.ToDouble(args[1]), 0);

                case "ln":
                    return Math.Log(Convert.ToDouble(args[1]));

                case "neg":
                    return -1 * Convert.ToDouble(args[1]);

                case "pos":
                    return +1 * Convert.ToDouble(args[1]);
            }

            return 0;
        }

        private static double identifier(string token)
        {
            switch (token.ToLower())
            {
                case "e":
                    return Math.E;
                case "pi":
                    return Math.PI;
                default:
                    // look in symbol table....?
                    return 0;
            }
        }

        private static bool Is_operator(string token, ref ArrayList m_operators, ref mcSymbol operatr, PRECEDENCE level = (PRECEDENCE)(-1))
        {
            try
            {
                mcSymbol op = new mcSymbol { Token = token, PrecedenceLevel = level, tag = "test" };

                int ir = m_operators.BinarySearch(op, op);

                if (ir > -1)
                {
                    operatr = (mcSymbol)m_operators[ir];
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
 private static bool is_function(string token, string[] m_funcs)
        {
            try
            {
                int lr = Array.BinarySearch(m_funcs, token.ToLower());

                return lr > -1;
            }
            catch
            {
                return false;
            }
        }

        public static bool calc_scan(string line, ref Queue symbols, ref int[,] m_State, ref string m_colstring, string[] m_funcs)
        {
            int sp = 0;  // start position marker
            int cp = 0;  // current position marker
            int col;     // input column
            int lex_state = 1;

            symbols = new Queue();

            line += " "; // add a space as an end marker

            while (cp <= line.Length - 1)
            {
                char cc = line[cp];

                col = m_colstring.IndexOf(cc) + 3;

                switch (col)
                {
                    case 2:
                        if (ALPHA.IndexOf(char.ToUpper(cc)) > 0)
                        {
                            col = 1;
                        }
                        else if (DIGITS.IndexOf(char.ToUpper(cc)) > 0)
                        {
                            col = 2;
                        }
                        else
                        {
                            col = 6;
                        }
                        break;

                    case > 5:
                        col = 7;
                        break;
                }

                lex_state = m_State[lex_state - 1, col - 1];

                switch (lex_state)
                {
                    case 3:
                        mcSymbol sym = new mcSymbol { Token = line.Substring(sp, cp - sp) };
                        sym.Cls = is_function(sym.Token, m_funcs) ? TOKENCLASS.KEYWORD : TOKENCLASS.IDENTIFIER;
                        symbols.Enqueue(sym);
                        lex_state = 1;
                        cp--;
                        break;

                    case 5:
                        sym = new mcSymbol { Token = line.Substring(sp, cp - sp), Cls = TOKENCLASS.NUMBER };
                        symbols.Enqueue(sym);
                        lex_state = 1;
                        cp--;
                        break;

                    case 6:
                        sym = new mcSymbol { Token = line.Substring(sp, cp - sp + 1), Cls = TOKENCLASS.PUNCTUATION };
                        symbols.Enqueue(sym);
                        lex_state = 1;
                        break;

                    case 7:
                        sym = new mcSymbol { Token = line.Substring(sp, cp - sp + 1), Cls = TOKENCLASS.OPERATR };
                        symbols.Enqueue(sym);
                        lex_state = 1;
                        break;
                }

                cp++;
                if (lex_state == 1) sp = cp;
            }

            return true;
        }
        private static void init(ref int[,] m_State, ref string m_colstring, string[] m_funcs, ref ArrayList m_operators)
        {
            int[,] state = {
        {2, 4, 1, 1, 4, 6, 7},
        {2, 3, 3, 3, 3, 3, 3},
        {1, 1, 1, 1, 1, 1, 1},
        {2, 4, 5, 5, 4, 5, 5},
        {1, 1, 1, 1, 1, 1, 1},
        {1, 1, 1, 1, 1, 1, 1},
        {1, 1, 1, 1, 1, 1, 1}
    };

            init_operators(ref m_operators);

            m_State = state;
            m_colstring = "\t .()";
            foreach (mcSymbol op in m_operators)
            {
                m_colstring += op.Token;
            }

            Array.Sort(m_funcs);
        }

        public Calc()
        {
            // init();
        }

        #region Recursive Descent Parsing Functions

        private static double level0(ref Queue tokens, ref ArrayList m_operators)
        {
            return level1(ref tokens, ref m_operators);
        }
       
private static double level1Prime(ref Queue tokens, double result, ref ArrayList m_operators)
        {
            mcSymbol symbol, operatr = null; ;

            if (tokens.Count > 0)
            {
                symbol = (mcSymbol)tokens.Peek();
            }
            else
            {
                return result;
            }

            // binary level1 precedence operators....+, -
            if (Is_operator(symbol.Token,ref m_operators, ref operatr, PRECEDENCE.LEVEL1))
            {
                tokens.Dequeue();
                result = Calc_op(operatr, result, level2(ref tokens, ref m_operators));
                result = level1Prime(ref tokens, result, ref m_operators);
            }

            return result;
        }
        private static double level1(ref Queue tokens, ref ArrayList m_operators)
        {
            return level1Prime(ref tokens, level2(ref tokens, ref m_operators),ref m_operators);
        }

        private static double level2(ref Queue tokens, ref ArrayList m_operators)
        {
            return level2_prime(ref tokens, level3(ref tokens, ref m_operators), ref m_operators);
        }

        private static double level2_prime(ref Queue tokens, double result, ref ArrayList m_operators)
        {
            mcSymbol symbol, operatr = null;

            if (tokens.Count > 0)
            {
                symbol = (mcSymbol)tokens.Peek();
            }
            else
            {
                return result;
            }

            // binary level2 precedence operators....*, /, \, %
            if (Is_operator(symbol.Token, ref m_operators, ref operatr, PRECEDENCE.LEVEL2))
            {
                tokens.Dequeue();
                result = Calc_op(operatr, result, level3(ref tokens, ref m_operators));
                result = level2_prime(ref tokens, result, ref m_operators);
            }

            return result;
        }

        private static double level3(ref Queue tokens, ref ArrayList m_operators)
        {
            return level3_prime(ref tokens, level4(ref tokens, ref m_operators), ref m_operators);
        }

        private static double level3_prime(ref Queue tokens, double result, ref ArrayList m_operators)
        {
            mcSymbol symbol, operatr = null;

            if (tokens.Count > 0)
            {
                symbol = (mcSymbol)tokens.Peek();
            }
            else
            {
                return result;
            }

            // binary level3 precedence operators....^
            if (Is_operator(symbol.Token, ref m_operators, ref  operatr, PRECEDENCE.LEVEL3))
            {
                tokens.Dequeue();
                result = Calc_op(operatr, result, level4(ref tokens, ref m_operators));
                result = level3_prime(ref tokens, result, ref m_operators);
            }

            return result;
        }

        private static double level4(ref Queue tokens, ref ArrayList m_operators)
        {
            return level4_prime(ref tokens, ref m_operators);
        }

        private static double level4_prime(ref Queue tokens, ref ArrayList m_operators)
        {
            mcSymbol symbol, operatr = null;

            if (tokens.Count > 0)
            {
                symbol = (mcSymbol)tokens.Peek();
            }
            else
            {
                throw new Exception("Invalid expression.");
            }

            // unary level4 precedence right associative operators.... +, -
            if (Is_operator(symbol.Token, ref m_operators, ref operatr, PRECEDENCE.LEVEL4))
            {
                tokens.Dequeue();
                return Calc_op(operatr, level5(tokens, ref m_operators));
            }
            else
            {
                return level5(tokens, ref m_operators);
            }
        }

        private static double level5(Queue tokens, ref ArrayList m_operators)
        {
            return level5_prime(tokens, level6(ref tokens, ref m_operators), ref m_operators);
        }

        private static double level5_prime(Queue tokens, double result, ref ArrayList m_operators)
        {
            mcSymbol symbol, operatr = null;

            if (tokens.Count > 0)
            {
                symbol = (mcSymbol)tokens.Peek();
            }
            else
            {
                return result;
            }

            // unary level5 precedence left associative operators.... !
            if (Is_operator(symbol.Token, ref m_operators, ref operatr, PRECEDENCE.LEVEL5))
            {
                tokens.Dequeue();
                return Calc_op(operatr, result);
            }
            else
            {
                return result;
            }
        }
        private static double level6(ref Queue tokens, ref ArrayList m_operators)
        {
            mcSymbol symbol;

            if (tokens.Count > 0)
            {
                symbol = (mcSymbol)tokens.Peek();
            }
            else
            {
                throw new Exception("Invalid expression.");
            }

            double val;

            // constants, identifiers, keywords, -> expressions
            if (symbol.Token == "(") // opening paren of new expression
            {
                tokens.Dequeue();
                val = level0(ref tokens, ref m_operators);

                symbol = (mcSymbol)tokens.Dequeue();
                // closing paren
                if (symbol.Token != ")") throw new Exception("Invalid expression.");

                return val;
            }
            else
            {
                switch (symbol.Cls)
                {
                    case TOKENCLASS.IDENTIFIER:
                        tokens.Dequeue();
                        return identifier(symbol.Token);

                    case TOKENCLASS.KEYWORD:
                        tokens.Dequeue();
                        return calc_function(symbol.Token, arguments(tokens, ref m_operators));

                    case TOKENCLASS.NUMBER:
                        tokens.Dequeue();
                        return Convert.ToDouble(symbol.Token);

                    default:
                        throw new Exception("Invalid expression.");
                }
            }
        }

        private static Collection arguments(Queue tokens, ref ArrayList m_operators)
        {
            mcSymbol symbol;
            Collection args = new Collection();

            if (tokens.Count > 0)
            {
                symbol = (mcSymbol)tokens.Peek();
            }
            else
            {
                throw new Exception("Invalid expression.");
            }

            if (symbol.Token == "(")
            {
                tokens.Dequeue();
                args.Add(level0(ref tokens, ref m_operators));

                symbol = (mcSymbol)tokens.Dequeue();
                while (symbol.Token != ")")
                {
                    if (symbol.Token == ",")
                    {
                        args.Add(level0(ref tokens, ref m_operators));
                    }
                    else
                    {
                        throw new Exception("Invalid expression.");
                    }
                    symbol = (mcSymbol)tokens.Dequeue();
                }

                return args;
            }
            else
            {
                throw new Exception("Invalid expression.");
            }
        }

        public static double Round(double Number, int NumDigitsAfterDecimal = 0)
        {
            return Math.Floor(Number * Math.Pow(10, NumDigitsAfterDecimal) + (0.500000000001 * Math.Sign(Number))) / Math.Pow(10, NumDigitsAfterDecimal);
        }
        #endregion
    }
}
