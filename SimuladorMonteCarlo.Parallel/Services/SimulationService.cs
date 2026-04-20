using SimuladorMonteCarlo.Parallel.Entities;
using SimuladorMonteCarlo.Parallel.Enums;
using System;
using System.Drawing;
using System.IO;
using System.Threading;

namespace SimulacionMonteCarlo.Parallel.Services
{
    public class SimulationService
    {
        private const int Width = 1000;
        private const int Height = 1000;
        private const int Days = 365;

        private const double InfectionProbability = 0.25;
        private const double RecoveryProbability = 0.05;
        private const double DeathProbability = 0.01;

        private CellState[,] currentGrid;
        private CellState[,] nextGrid;

        private readonly int numberOfThreads;
        private readonly ThreadLocal<Random> random;

        public SimulationService(int threads = 4)
        {
            numberOfThreads = threads;

            currentGrid = new CellState[Width, Height];
            nextGrid = new CellState[Width, Height];

            random = new ThreadLocal<Random>(
                delegate
                {
                    return new Random(Guid.NewGuid().GetHashCode());
                });
        }

        public void Run()
        {
            InitializeGrid();
            SeedInitialInfections(10);

            string outputFolder = "../../../output";
            string framesFolder = Path.Combine(outputFolder, "frames");
            string statsPath = Path.Combine(outputFolder, "stats_parallel.csv");

            Directory.CreateDirectory(outputFolder);
            Directory.CreateDirectory(framesFolder);

            File.WriteAllText(
                statsPath,
                "day,susceptible,infected,recovered,dead,r0\n");

            int previousInfected = 10;

            for (int day = 0; day < Days; day++)
            {
                int newInfections = UpdateDayParallel();

                Statistics stats = ComputeStatisticsParallel();

                if (previousInfected > 0)
                {
                    stats.R0 = (double)newInfections / previousInfected;
                }
                else
                {
                    stats.R0 = 0;
                }

                previousInfected = stats.Infected;

                SaveStatistics(day, stats, statsPath);
                SaveFrame(day, framesFolder);

                SwapBuffers();

                Console.WriteLine(
                    string.Format(
                        "[PARALLEL] Día {0}/{1} | S: {2} | I: {3} | R: {4} | D: {5} | R0: {6:F2}",
                        day + 1,
                        Days,
                        stats.Susceptible,
                        stats.Infected,
                        stats.Recovered,
                        stats.Dead,
                        stats.R0));
            }
        }

        private void InitializeGrid()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    currentGrid[x, y] = CellState.Susceptible;
                    nextGrid[x, y] = CellState.Susceptible;
                }
            }
        }

        private void SeedInitialInfections(int count)
        {
            int infected = 0;
            Random seedRandom = new Random();

            while (infected < count)
            {
                int x = seedRandom.Next(Width);
                int y = seedRandom.Next(Height);

                if (currentGrid[x, y] == CellState.Susceptible)
                {
                    currentGrid[x, y] = CellState.Infected;
                    infected++;
                }
            }
        }

        private int UpdateDayParallel()
        {
            int totalNewInfections = 0;

            System.Threading.Tasks.Parallel.For(
                0,
                numberOfThreads,
                delegate (int threadIndex)
                {
                    int localNewInfections = 0;
                    Random localRandom = random.Value;

                    int rowsPerThread = Height / numberOfThreads;

                    int startRow = threadIndex * rowsPerThread;
                    int endRow;

                    if (threadIndex == numberOfThreads - 1)
                    {
                        endRow = Height;
                    }
                    else
                    {
                        endRow = startRow + rowsPerThread;
                    }

                    for (int x = 0; x < Width; x++)
                    {
                        for (int y = startRow; y < endRow; y++)
                        {
                            CellState current = currentGrid[x, y];

                            switch (current)
                            {
                                case CellState.Susceptible:

                                    int infectedNeighbors =
                                        CountInfectedNeighbors(x, y);

                                    if (infectedNeighbors > 0 &&
                                        localRandom.NextDouble() < InfectionProbability)
                                    {
                                        nextGrid[x, y] = CellState.Infected;
                                        localNewInfections++;
                                    }
                                    else
                                    {
                                        nextGrid[x, y] = CellState.Susceptible;
                                    }

                                    break;

                                case CellState.Infected:

                                    double roll = localRandom.NextDouble();

                                    if (roll < DeathProbability)
                                    {
                                        nextGrid[x, y] = CellState.Dead;
                                    }
                                    else if (roll < DeathProbability + RecoveryProbability)
                                    {
                                        nextGrid[x, y] = CellState.Recovered;
                                    }
                                    else
                                    {
                                        nextGrid[x, y] = CellState.Infected;
                                    }

                                    break;

                                case CellState.Recovered:
                                    nextGrid[x, y] = CellState.Recovered;
                                    break;

                                case CellState.Dead:
                                    nextGrid[x, y] = CellState.Dead;
                                    break;
                            }
                        }
                    }

                    Interlocked.Add(ref totalNewInfections, localNewInfections);
                });

            return totalNewInfections;
        }

        private int CountInfectedNeighbors(int x, int y)
        {
            int count = 0;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 &&
                        nx < Width &&
                        ny >= 0 &&
                        ny < Height &&
                        currentGrid[nx, ny] == CellState.Infected)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private Statistics ComputeStatisticsParallel()
        {
            Statistics globalStats = new Statistics();
            object lockObject = new object();

            System.Threading.Tasks.Parallel.For(
                0,
                numberOfThreads,
                delegate (int threadIndex)
                {
                    Statistics localStats = new Statistics();

                    int rowsPerThread = Height / numberOfThreads;

                    int startRow = threadIndex * rowsPerThread;
                    int endRow;

                    if (threadIndex == numberOfThreads - 1)
                    {
                        endRow = Height;
                    }
                    else
                    {
                        endRow = startRow + rowsPerThread;
                    }

                    for (int x = 0; x < Width; x++)
                    {
                        for (int y = startRow; y < endRow; y++)
                        {
                            switch (nextGrid[x, y])
                            {
                                case CellState.Susceptible:
                                    localStats.Susceptible++;
                                    break;

                                case CellState.Infected:
                                    localStats.Infected++;
                                    break;

                                case CellState.Recovered:
                                    localStats.Recovered++;
                                    break;

                                case CellState.Dead:
                                    localStats.Dead++;
                                    break;
                            }
                        }
                    }

                    lock (lockObject)
                    {
                        globalStats.Susceptible += localStats.Susceptible;
                        globalStats.Infected += localStats.Infected;
                        globalStats.Recovered += localStats.Recovered;
                        globalStats.Dead += localStats.Dead;
                    }
                });

            return globalStats;
        }

        private void SaveStatistics(int day, Statistics stats, string path)
        {
            File.AppendAllText(
                path,
                string.Format(
                    "{0},{1},{2},{3},{4},{5:F2}\n",
                    day,
                    stats.Susceptible,
                    stats.Infected,
                    stats.Recovered,
                    stats.Dead,
                    stats.R0));
        }

        private void SaveFrame(int day, string framesFolder)
        {
            using (Bitmap bitmap = new Bitmap(Width, Height))
            {
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        Color color;

                        switch (currentGrid[x, y])
                        {
                            case CellState.Susceptible:
                                color = Color.White;
                                break;

                            case CellState.Infected:
                                color = Color.Red;
                                break;

                            case CellState.Recovered:
                                color = Color.Green;
                                break;

                            case CellState.Dead:
                                color = Color.Black;
                                break;

                            default:
                                color = Color.White;
                                break;
                        }

                        bitmap.SetPixel(x, y, color);
                    }
                }

                string filePath = Path.Combine(
                    framesFolder,
                    string.Format("parallel_day_{0:D3}.png", day));

                bitmap.Save(filePath);
            }
        }

        private void SwapBuffers()
        {
            CellState[,] temp = currentGrid;
            currentGrid = nextGrid;
            nextGrid = temp;
        }
    }
}