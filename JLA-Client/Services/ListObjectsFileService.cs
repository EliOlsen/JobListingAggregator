using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace JLAClient.Services;

public class ListObjectsFileService<T>()
{
    public string? filePath;
    public void SetFilePath(string newFilePath)
    {
        filePath = newFilePath;
    }
    /// <summary>
    /// Stores the given list into a file on disc
    /// </summary>
    /// <param name="listToSave">The list of objects to save</param>
    public async Task SaveToFileAsync(IEnumerable<T> listToSave)
    {
        if (filePath is null) return;
        // Ensure all directories exists
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        // We use a FileStream to write all listings to disc
        using var fs = File.Create(filePath);
        await JsonSerializer.SerializeAsync(fs, listToSave);
    }
    /// <summary>
    /// Loads the file from disc and returns the list of objects stored inside
    /// </summary>
    /// <returns>An IEnumerable of objects loaded or null in case the file was not found</returns>
    public async Task<IEnumerable<T>?> LoadFromFileAsync()
    {
        if (filePath is null) return null;
        try
        {
            // We try to read the saved file and return the list of objects if successful
            using var fs = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<IEnumerable<T>>(fs);
        }
        catch (Exception e) when (e is FileNotFoundException || e is DirectoryNotFoundException)
        {
            // In case the file was not found, we simply return null
            return null;
        }
    }
    /// <summary>
    /// Retrieves the last time the file was saved
    /// </summary>
    /// <returns>The DateTime of the last time file saved or null in case the file was not found</returns>
    public DateTime? GetLastTimeListingsSavedToFile()
    {
        if (filePath is null) return null;
        try
        {
            return File.GetLastWriteTime(filePath);
        }
        catch (Exception)
        {
            return null;
        }
    }
}