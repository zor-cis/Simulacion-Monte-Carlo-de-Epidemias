using SimuladorMonteCarlo.Sequential.Entities;
using SimuladorMonteCarlo.Sequential.Enums;
using System;
using System.Drawing;
using System.IO;

namespace SimulacionMonteCarlo.Sequential.Services
{
    public class SimulationService
    {
        private const int Width = 1000;
        private const int Height = 1000;
        private const int Days = 365;

        private const double InfectionProbability = 0.25;
        private const double RecoveryProbability = 0.05;
        private const double DeathProbability = 0.01;

        private readonly Random random = new Random();

        private CellState[,] currentGrid;
        private CellState[,] nextGrid;

        public SimulationService()
        {
            currentGrid = new CellState[Width, Height];
            nextGrid = new CellState[Width, Height];
        }

        public void Run()
        {
            InitializeGrid();
            SeedInitialInfections(10);

            string outputFolder = "../../../output";
            string framesFolder = Path.Combine(outputFolder, "frames");
            string statsPath = Path.Combine(outputFolder, "stats_sequential.csv");

            Directory.CreateDirectory(outputFolder);
            Directory.CreateDirectory(framesFolder);

            File.WriteAllText(
                statsPath,
                "day,susceptible,infected,recovered,dead,r0\n");

            int previousInfected = 10;

            for (int day = 0; day < Days; day++)
            {
                int newInfections = UpdateDay();

                Statistics stats = ComputeStatistics();

                stats.R0 = previousInfected > 0
                    ? (double)newInfections / previousInfected
                    : 0;

                previousInfected = stats.Infected;

                SaveStatistics(day, stats, statsPath);
                SaveFrame(day, framesFolder);

                SwapBuffers();

                Console.WriteLine(
                    string.Format(
                        "Día {0}/{1} | S: {2} | I: {3} | R: {4} | D: {5} | R0: {6:F2}",
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

            while (infected < count)
            {
                int x = random.Next(Width);
                int y = random.Next(Height);

                if (currentGrid[x, y] == CellState.Susceptible)
                {
                    currentGrid[x, y] = CellState.Infected;
                    infected++;
                }
            }
        }

        private int UpdateDay()
        {
            int newInfections = 0;

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    CellState current = currentGrid[x, y];

                    switch (current)
                    {
                        case CellState.Susceptible:

                            int infectedNeighbors = CountInfectedNeighbors(x, y);

                            if (infectedNeighbors > 0 &&
                                random.NextDouble() < InfectionProbability)
                            {
                                nextGrid[x, y] = CellState.Infected;
                                newInfections++;
                            }
                            else
                            {
                                nextGrid[x, y] = CellState.Susceptible;
                            }

                            break;

                        case CellState.Infected:

                            double roll = random.NextDouble();

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

            return newInfections;
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

        private Statistics ComputeStatistics()
        {
            Statistics stats = new Statistics();

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    switch (nextGrid[x, y])
                    {
                        case CellState.Susceptible:
                            stats.Susceptible++;
                            break;

                        case CellState.Infected:
                            stats.Infected++;
                            break;

                        case CellState.Recovered:
                            stats.Recovered++;
                            break;

                        case CellState.Dead:
                            stats.Dead++;
                            break;
                    }
                }
            }

            return stats;
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
                    string.Format("sequential_day_{0:D3}.png", day));

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