using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Helpers
{
    public static class FileHelper
    {
        public static async Task<string?> SaveFileAsync(IFormFile? file, string webRootPath, string subFolder)
        {
            if (file == null || file.Length == 0) return null;

            try
            {
                // المسار: wwwroot/uploads/reports
                var uploadsFolder = Path.Combine(webRootPath, "uploads", subFolder);

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // اسم فريد
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return $"/uploads/{subFolder}/{fileName}";
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
