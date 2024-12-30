using ArchipelagoDiscordClient.Constants;
using System.Text.Json;

namespace ArchipelagoDiscordClient.Services
{
	public class JsonFileService : IFileService
	{
		public async Task<T?> ReadFileAsync<T>(string fileName)
		{
			try
			{
				var fullPath = GetFilePath(fileName);
				if (!File.Exists(fullPath))
				{
					throw new FileNotFoundException("File not found", fullPath);
				}

				var jsonContent = await File.ReadAllTextAsync(fullPath);

				Console.WriteLine($"JSON content to deserialize: {jsonContent}");
				jsonContent = jsonContent.Trim();

				if (string.IsNullOrWhiteSpace(jsonContent))
				{
					return default; // Return default (null for reference types) if file is empty
				}

				return JsonSerializer.Deserialize<T>(jsonContent, new JsonSerializerOptions {PropertyNameCaseInsensitive = true});
			}
			catch (JsonException ex)
			{
				Console.WriteLine($"Error reading JSON from file: {ex.Message}");
				return default;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error reading file: {ex.Message}");
				throw;
			}
		}

		public async Task WriteFileAsync<T>(string fileName, T data)
		{
			try
			{
				var fullPath = GetFilePath(fileName);
				EnsureDirectoryExists(fullPath);

				var jsonContent = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
				await File.WriteAllTextAsync(fullPath, jsonContent);

				Console.WriteLine($"File updated or new item added for {data}.");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error updating or adding data: {ex.Message}");
				throw;
			}
		}

		private string GetFilePath(string fileName)
		{
			return Path.Combine(FilePaths.BaseFilePath, fileName);
		}

		private void EnsureDirectoryExists(string fullPath)
		{
			var directory = Path.GetDirectoryName(fullPath);
			if (directory != null && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}
		}
	}
}
