using HealthAssistant.Models;

namespace HealthAssistant.Services
{
    /// <summary>
    /// Data store interface for CRUD on the given model
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IDataStore<T>
    {
        Task<bool> AddItemAsync(T Item);
        Task<bool> UpdateItemAsync(T Item);
        Task<bool> DeleteItemAsync(MeasuredItem Item);
        Task<T> GetItemAsync(string Dd);
        Task<IEnumerable<T>> GetItemsAsync(Measurement Type);
    }
}
