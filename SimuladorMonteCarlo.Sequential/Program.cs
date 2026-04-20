using SimulacionMonteCarlo.Sequential.Services;
using System;

namespace SimuladorMonteCarlo.Sequential
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  SIMULACIÓN MONTE-CARLO SIR SECUENCIAL ");
            Console.WriteLine("========================================");
            Console.WriteLine();

            Console.WriteLine("Iniciando simulación secuencial...");
            Console.WriteLine();

            DateTime start = DateTime.Now;

            SimulationService simulationService = new SimulationService();
            simulationService.Run();

            DateTime end = DateTime.Now;
            TimeSpan duration = end - start;

            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("Simulación terminada.");
            Console.WriteLine("Tiempo total: {0:F2} segundos", duration.TotalSeconds);
            Console.WriteLine("Resultados guardados en la carpeta output/");
            Console.WriteLine("========================================");

            Console.WriteLine();
            Console.WriteLine("Presiona cualquier tecla para salir...");
            Console.ReadKey();
        }
    }
}
