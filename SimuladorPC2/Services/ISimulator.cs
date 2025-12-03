using System.Collections.Generic;

namespace SimuladorPC2.Services
{
 public record Metrics(long Cycles, long Instructions, long CacheHits, long CacheMisses)
 {
 public double Ipc => Cycles ==0 ?0 : (double)Instructions / Cycles;
 }

 public interface ISimulator
 {
 void LoadAssembly(IEnumerable<string> lines);
 bool Step();
 void Run(long maxCycles =1_000_000);
 IReadOnlyList<string> GetLogs();
 int[] GetRegisters();
 int[] GetMemorySnapshot(int length =64);
 Metrics GetMetrics();
 void Reset();
 }
}
