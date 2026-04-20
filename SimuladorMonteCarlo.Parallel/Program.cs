using SimulacionMonteCarlo.Parallel.Services;
using System;

namespace SimuladorMonteCarlo.Parallel
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("   SIMULACIÓN MONTE-CARLO SIR PARALELA  ");
            Console.WriteLine("========================================");
            Console.WriteLine();

            Console.Write("Cantidad de hilos a utilizar (1, 2, 4, 8): ");

            int threads;

            while (!int.TryParse(Console.ReadLine(), out threads) ||
                   (threads != 1 && threads != 2 && threads != 4 && threads != 8))
            {
                Console.WriteLine("Valor inválido. Escribe 1, 2, 4 u 8.");
                Console.Write("Cantidad de hilos: ");
            }

            Console.WriteLine();
            Console.WriteLine("Iniciando simulación con {0} hilos...", threads);
            Console.WriteLine();

            DateTime start = DateTime.Now;

            SimulationService simulationService = new SimulationService(threads);
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
