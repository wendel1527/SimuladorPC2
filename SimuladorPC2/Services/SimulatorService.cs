using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SimuladorPC2.Services
{
 public class SimulatorService : ISimulator
 {
 private readonly List<string> _program = new();
 private readonly List<string> _logs = new();
 private int[] _regs = new int[16];
 private int[] _mem;
 private int _pc;
 private long _cycles;
 private long _instrCount;
 private long _cacheHits;
 private long _cacheMisses;

 private readonly Regex _rxLi = new(@"^\s*LI\s+x(\d{1,2})\s*,\s*([+-]?\d+)\s*$", RegexOptions.IgnoreCase);
 private readonly Regex _rxAdd = new(@"^\s*ADD\s+x(\d{1,2})\s*,\s*x(\d{1,2})\s*,\s*x(\d{1,2})\s*$", RegexOptions.IgnoreCase);
 private readonly Regex _rxSub = new(@"^\s*SUB\s+x(\d{1,2})\s*,\s*x(\d{1,2})\s*,\s*x(\d{1,2})\s*$", RegexOptions.IgnoreCase);
 private readonly Regex _rxSw = new(@"^\s*SW\s+x(\d{1,2})\s*,\s*([+-]?\d+)\(x(\d{1,2})\)\s*$", RegexOptions.IgnoreCase);
 private readonly Regex _rxLw = new(@"^\s*LW\s+x(\d{1,2})\s*,\s*([+-]?\d+)\(x(\d{1,2})\)\s*$", RegexOptions.IgnoreCase);

 public SimulatorService()
 {
 _mem = new int[1024];
 Reset();
 }

 public void LoadAssembly(IEnumerable<string> lines)
 {
 _program.Clear();
 foreach (var l in lines)
 {
 var t = l.Trim();
 if (string.IsNullOrEmpty(t) || t.StartsWith("#")) continue;
 _program.Add(t);
 }

 _logs.Add($"Program loaded: {_program.Count} lines");
 _pc =0;
 }

 public bool Step()
 {
 if (_pc <0 || _pc >= _program.Count)
 {
 _logs.Add($"PC out of range ({_pc}). Halting.");
 return false;
 }

 var inst = _program[_pc];
 _logs.Add($"Cycle {_cycles +1}: PC={_pc} Instr=\"{inst}\"");
 _cycles++;

 if (TryMatch(_rxLi, inst, out var mLi))
 {
 var rd = int.Parse(mLi.Groups[1].Value);
 var imm = int.Parse(mLi.Groups[2].Value);
 if (rd !=0) _regs[rd] = imm;
 _instrCount++;
 _logs.Add($" LI x{rd} = {imm}");
 }
 else if (TryMatch(_rxAdd, inst, out var mAdd))
 {
 var rd = int.Parse(mAdd.Groups[1].Value);
 var rs1 = int.Parse(mAdd.Groups[2].Value);
 var rs2 = int.Parse(mAdd.Groups[3].Value);
 if (rd !=0) _regs[rd] = _regs[rs1] + _regs[rs2];
 _instrCount++;
 _logs.Add($" ADD x{rd} = x{rs1}({_regs[rs1]}) + x{rs2}({_regs[rs2]}) => {_regs[rd]}");
 }
 else if (TryMatch(_rxSub, inst, out var mSub))
 {
 var rd = int.Parse(mSub.Groups[1].Value);
 var rs1 = int.Parse(mSub.Groups[2].Value);
 var rs2 = int.Parse(mSub.Groups[3].Value);
 if (rd !=0) _regs[rd] = _regs[rs1] - _regs[rs2];
 _instrCount++;
 _logs.Add($" SUB x{rd} = {_regs[rd]}");
 }
 else if (TryMatch(_rxSw, inst, out var mSw))
 {
 var rs = int.Parse(mSw.Groups[1].Value);
 var off = int.Parse(mSw.Groups[2].Value);
 var baseReg = int.Parse(mSw.Groups[3].Value);
 var addr = _regs[baseReg] + off;
 WriteMem(addr, _regs[rs]);
 _instrCount++;
 _logs.Add($" SW mem[{addr}] = x{rs}({_regs[rs]})");
 }
 else if (TryMatch(_rxLw, inst, out var mLw))
 {
 var rd = int.Parse(mLw.Groups[1].Value);
 var off = int.Parse(mLw.Groups[2].Value);
 var baseReg = int.Parse(mLw.Groups[3].Value);
 var addr = _regs[baseReg] + off;
 var val = ReadMem(addr);
 if (rd !=0) _regs[rd] = val;
 _instrCount++;
 _logs.Add($" LW x{rd} = mem[{addr}] ({val})");
 }
 else if (inst.Equals("NOP", StringComparison.OrdinalIgnoreCase))
 {
 _instrCount++;
 _logs.Add(" NOP");
 }
 else if (inst.Equals("HALT", StringComparison.OrdinalIgnoreCase))
 {
 _logs.Add(" HALT encountered. Stopping.");
 return false;
 }
 else
 {
 _logs.Add($" Unknown instruction: {inst} -> treated as NOP");
 _instrCount++;
 }

 _pc++;
 return true;
 }

 public void Run(long maxCycles =1_000_000)
 {
 long startCycles = _cycles;
 while (_cycles - startCycles < maxCycles)
 {
 if (!Step()) break;
 }
 }

 public IReadOnlyList<string> GetLogs() => _logs.AsReadOnly();

 public int[] GetRegisters() => (int[])_regs.Clone();

 public int[] GetMemorySnapshot(int length =64) => _mem.Take(Math.Min(length, _mem.Length)).ToArray();

 public Metrics GetMetrics() => new Metrics(_cycles, _instrCount, _cacheHits, _cacheMisses);

 public void Reset()
 {
 _regs = new int[16];
 _mem = new int[1024];
 _pc =0;
 _cycles =0;
 _instrCount =0;
 _cacheHits =0;
 _cacheMisses =0;
 _logs.Clear();
 _logs.Add("Simulator reset.");
 }

 private int ReadMem(int addr)
 {
 if (addr <0 || addr >= _mem.Length) return 0;
 if (addr %16 ==0) _cacheMisses++; else _cacheHits++;
 return _mem[addr];
 }

 private void WriteMem(int addr, int value)
 {
 if (addr <0 || addr >= _mem.Length) return;
 if (addr %16 ==0) _cacheMisses++; else _cacheHits++;
 _mem[addr] = value;
 }

 private static bool TryMatch(Regex rx, string input, out Match m)
 {
 m = rx.Match(input);
 return m.Success;
 }
 }
}
