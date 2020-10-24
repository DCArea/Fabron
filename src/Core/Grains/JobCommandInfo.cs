using Orleans.Concurrency;

namespace TGH.Grains
{
    [Immutable]
    public class JobCommandInfo
    {
        public JobCommandInfo(string name, string data)
        {
            Name = name;
            Data = data;
        }
        public string Name { get; }
        public string Data { get; }
    }
}
