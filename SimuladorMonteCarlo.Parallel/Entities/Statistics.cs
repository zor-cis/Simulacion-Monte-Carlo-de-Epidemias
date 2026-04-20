namespace SimuladorMonteCarlo.Parallel.Entities
{
    public class Statistics
    {
        public int Susceptible { get; set; }
        public int Infected { get; set; }
        public int Recovered { get; set; }
        public int Dead { get; set; }
        public double R0 { get; set; }
    }
}
