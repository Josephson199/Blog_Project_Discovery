using DataStore.Models;

namespace Infrastructure.Interfaces
{
    public interface IOutputStrategy
    {
        string Transform(string markdown);
    }
}
