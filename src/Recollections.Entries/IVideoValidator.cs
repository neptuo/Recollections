using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public interface IVideoValidator
    {
        Task ValidateAsync(string userId, IFileInput file);
    }
}
