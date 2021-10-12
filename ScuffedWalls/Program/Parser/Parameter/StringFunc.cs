﻿using ModChart;
using ModChart.Wall;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ScuffedWalls
{
    public class StringFunction
    {
        public static Random RandomInstance = new Random();
        public string Name { get; set; } //name of the func
        public Func<ValuePair<string[], string>, string> FunctionAction { get; set; } //convert from params to output string
        public static Func<StringFunction, string> Exposer => sfunc => sfunc.Name;
        public static StringFunction[] Functions => new StringFunction[]
            {
                new StringFunction()
                {
                    Name = "RepeatPointDefinition",
                    FunctionAction = InputArgs =>
                    {
                        int indexoflast = InputArgs.Extra.LastIndexOf(",");

                        string pd = InputArgs.Extra.Substring(0,indexoflast);

                        int repcount = int.Parse(InputArgs.Extra.Substring(indexoflast + 1,InputArgs.Extra.Length - indexoflast - 1));

                        Lookup<AssignableInlineVariable> vars = new Lookup<AssignableInlineVariable>(AssignableInlineVariable.Exposer);
                        AssignableInlineVariable repeat = new AssignableInlineVariable("reppd", "0");
                        vars.Add(repeat);
                        StringComputationExcecuter computer = new StringComputationExcecuter(vars, true);

                        List<string> points = new List<string>();
                        for (int i = 0; i < repcount; i++)
                        {
                            repeat.StringData = i.ToString();
                            points.Add(computer.Parse(pd));
                            Parameter.ExternalVariables.RefreshAllParameters();
                        }

                        return string.Join(',',points);
                    }
                },
                new StringFunction()
                {
                    Name = "MultPointDefinition",
                    FunctionAction = InputArgs =>
                    {
                        string[] spli = InputArgs.Extra.Split("],",2);
                        string pd = spli[0] + "]";
                        float val = spli[1].ToFloat();

                        object[] PointDefinition = JsonSerializer.Deserialize<object[]>(pd);
                        for(int i = 0; i < PointDefinition.Length; i++) if(float.TryParse(PointDefinition[i].ToString(),out float result)) PointDefinition[i] = result * val;

                        return JsonSerializer.Serialize(PointDefinition);
                    }
                },
                new StringFunction()
                {
                    Name = "HSLtoRGB",
                    FunctionAction = InputArgs =>
                    {
                        float H = InputArgs.Main[0].ToFloat();
                        float S = InputArgs.Main.Length > 1 ? InputArgs.Main[1].ToFloat() : 1f;
                        float L = InputArgs.Main.Length > 2 ? InputArgs.Main[2].ToFloat() : 0.5f;
                        float A = InputArgs.Main.Length > 3 ? InputArgs.Main[3].ToFloat() : 1f;
                        string AdditionalValues = InputArgs.Main.Length > 4 ? ","+ string.Join(',', InputArgs.Main.Slice(4,InputArgs.Main.Length)) : string.Empty;

                        Color p = Color.HslToRGB(H,S,L);

                        return $"[{p.R},{p.G},{p.B},{A}{AdditionalValues}]";
                    }
                },
                new StringFunction()
                {
                    Name = "OrderPointDefinitions",
                    FunctionAction = InputArgs =>
                    {
                        object[][] PointDefinition = JsonSerializer.Deserialize<object[][]>($"[{InputArgs.Extra}]");
                        string serial = JsonSerializer.Serialize( PointDefinition.OrderBy(p => gettimevalue(p)));
                        return serial.Substring(1,serial.Length-2);

                        float gettimevalue(object[] point)
                        {
                            float last = 0;
                            foreach(var val in point) if(float.TryParse(val.ToString(),out float result)) last = result;
                            return last;
                        }
                    }
                },
                new StringFunction()
                {
                    Name = "Random",
                    FunctionAction = InputArgs =>
                    {

                         float first = InputArgs.Main[0].ToFloat();
                         float last = InputArgs.Main[1].ToFloat();
                         if (InputArgs.Main.Length > 2) 
                         if (last < first)
                         {
                             float f = first;
                             float l = last;
                             first = l;
                             last = f;
                         }

                         return (RandomInstance.NextDouble() * (last - first) + first).ToString();

                    }
                },
                new StringFunction()
                {
                    Name = "RandomInt",
                    FunctionAction = InputArgs =>
                    {
                        Random rnd = new Random();
                        int first = int.Parse(InputArgs.Main[0]);
                        int last = int.Parse(InputArgs.Main[1]);
                        if (last < first)
                        {
                            int f = first;
                            int l = last;
                            first = l;
                            last = f;
                        }

                        return rnd.Next(first,last).ToString();
                    }
                }
        };

    }
    public class BracketAnalyzer
    {
        public string TextBeforeFocused;
        public string TextAfterFocused;
        public string TextInsideOfBrackets;
        public string TextInsideWithBrackets;
        public char OpeningBracket;
        public char ClosingBracket;
        public string FullLine;

        public BracketAnalyzer(string Line, char Opening, char Closing)
        {
            FullLine = TextInsideOfBrackets = TextInsideWithBrackets = Line;
            OpeningBracket = Opening;
            ClosingBracket = Closing;
        }
        public void Focus(int Index)
        {
            int closing = GetPosOfClosingSymbol(Index);
            var splits = SplitAt2(FullLine, Index, closing);
            TextInsideWithBrackets = splits[1];
            TextInsideOfBrackets = TextInsideWithBrackets.Substring(1, TextInsideWithBrackets.Length - 2);
            TextBeforeFocused = splits[0];
            TextAfterFocused = splits[2];
        }
        public void FocusFirst()
        {
            Focus(FullLine.IndexOf(OpeningBracket));
        }
        public void FocusFirst(string Name)
        {
            Focus(FullLine.IndexOf(Name) + (Name.Length - 1));
        }

        public int GetPosOfClosingSymbol(int indexofparenthesis)
        {
            char[] characters = FullLine.ToCharArray();
            int depth = 0;
            for (int i = indexofparenthesis; i < characters.Length; i++)
            {
                if (characters[i] == OpeningBracket) depth++;
                else if (characters[i] == ClosingBracket) depth--;

                if (depth == 0) return i;
            }
            throw new Exception("No closing of brackets/paranthesis!");
        }
        public static string[] SplitAt2(string s, int argpos, int argpos2) => new string[] { s.Substring(0, argpos), s.Substring(argpos, argpos2 - argpos + 1), s.Substring(argpos2 + 1) };
    }
}
