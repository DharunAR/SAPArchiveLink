namespace SAPArchiveLink
{
    public class ArchiveCounter
    {
        public int CreateCount { get; private set; }
        public int DeleteCount { get; private set; }
        public int UpdateCount { get; private set; }
        public int ViewCount { get; private set; }

        public void IncrementCreate(int value) => CreateCount += value;
        public void IncrementDelete(int value) => DeleteCount += value;
        public void IncrementUpdate(int value) => UpdateCount += value;
        public void IncrementView(int value) => ViewCount += value;
    }

}
