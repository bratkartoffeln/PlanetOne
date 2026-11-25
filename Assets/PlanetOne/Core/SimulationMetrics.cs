using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace PlanetOne.Core
{
    public enum SimulationStage { Commands,Economy,Logistics,Units,Space,Invasion,Ai,Victory }
    /// <summary>Opt-in wall-clock profiling, never part of the simulation or its saves.</summary>
    public sealed class SimulationMetrics
    {
        private readonly double[] current=new double[8];
        private readonly List<double>[] samples=new List<double>[8];
        public readonly List<double> TickMilliseconds=new List<double>();
        public SimulationMetrics(){for(int i=0;i<samples.Length;i++)samples[i]=new List<double>();}
        internal long Record(SimulationStage stage,long from)
        {long now=Stopwatch.GetTimestamp();current[(int)stage]+=(now-from)*1000.0/Stopwatch.Frequency;return now;}
        internal void End(long from)
        {
            TickMilliseconds.Add((Stopwatch.GetTimestamp()-from)*1000.0/Stopwatch.Frequency);
            for(int i=0;i<samples.Length;i++){samples[i].Add(current[i]);current[i]=0;}
        }
        public static double Percentile(List<double> values,double percentile)
        {if(values.Count==0)return 0;var copy=values.ToArray();Array.Sort(copy);return copy[Math.Min(copy.Length-1,(int)Math.Ceiling(copy.Length*percentile)-1)];}
        private static string Line(string name,List<double> values)
        {double total=0;foreach(double v in values)total+=v;return string.Format(CultureInfo.InvariantCulture,"{0},{1},{2:F4},{3:F4},{4:F4}\n",name,values.Count,values.Count==0?0:total/values.Count,Percentile(values,.95),Percentile(values,1));}
        public string Csv()
        {
            var text=new StringBuilder("stage,samples,mean_ms,p95_ms,max_ms\n");text.Append(Line("Total",TickMilliseconds));
            for(int i=0;i<samples.Length;i++)text.Append(Line(((SimulationStage)i).ToString(),samples[i]));
            text.Append(Line("AiOverOneMillisecond",samples[(int)SimulationStage.Ai].FindAll(v=>v>=1)));
            return text.ToString();
        }
    }
}
