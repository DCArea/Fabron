namespace TGH.Grains.TransientJob
{
    public enum JobStatus
    {
        NotCreated,
        Created,
        Running,
        RanToCompletion,
        Canceled,
        Faulted
    }
}
