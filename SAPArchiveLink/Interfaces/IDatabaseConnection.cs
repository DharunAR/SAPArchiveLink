using TRIM.SDK;

namespace SAPArchiveLink
{
    public interface IDatabaseConnection
    {
        ITrimRepository GetDatabase();
    }

}