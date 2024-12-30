
namespace ArchipelagoDiscordClient.Services
{
	public interface IFileService
	{
		Task WriteFileAsync<T>(string fileName, T data);

		Task<T?> ReadFileAsync<T>(string fileName);
	}
}