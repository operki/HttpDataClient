using System.Text.RegularExpressions;

namespace HttpDataClient.Helpers;

internal static class LocalHelper
{
    private static readonly Regex RemoveInvalidRegex = new Regex($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()))}]");

    public static void TryClearDir(string dir, int? skipFiles = null)
    {
        try
        {
            if(!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                return;
            }

            var dirFiles = Directory.GetFiles(dir);
            if(skipFiles == null)
                foreach(var file in dirFiles)
                    File.Delete(file);
            else
            {
                foreach(var file in dirFiles.OrderByDescending(file => file)
                            .Skip(skipFiles.Value))
                    File.Delete(file);
            }
        }
        catch(UnauthorizedAccessException) { }
        catch(IOException) { }
        catch(Exception e)
        {
            throw new Exception($"Can't clear folder '{dir}'. Exception: {e}");
        }
    }

    /// <summary>
    /// Убирает из файла непечатаемые символы для локального сохранения
    /// </summary>
    /// <param name="fileName">Файл для изменения</param>
    /// <returns>Файл для локального сохранения</returns>
    public static string GetSafeFileName(string fileName)
    {
        return RemoveInvalidRegex.Replace(Path.GetFileName(fileName), "");
    }
}
