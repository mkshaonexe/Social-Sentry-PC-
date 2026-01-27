using System;
using System.Drawing;
using System.IO;

namespace IconConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: IconConverter <input.png> <output.ico>");
                return;
            }

            string inputPath = args[0];
            string outputPath = args[1];

            try
            {
                // Note: On Windows, System.Drawing.Common uses GDI+.
                // PNG to ICO conversion via GDI+ can be tricky for multi-res, 
                // but this helper should work for a basic conversion.
                using (Bitmap bitmap = (Bitmap)Image.FromFile(inputPath))
                {
                    // Scale to 256x256 if needed
                    using (Bitmap scaled = new Bitmap(bitmap, new Size(256, 256)))
                    {
                        IntPtr hIcon = scaled.GetHicon();
                        using (Icon icon = Icon.FromHandle(hIcon))
                        {
                            using (FileStream fs = new FileStream(outputPath, FileMode.Create))
                            {
                                icon.Save(fs);
                            }
                        }
                    }
                }
                Console.WriteLine("✓ Successfully converted to " + outputPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("✗ Error: " + ex.Message);
            }
        }
    }
}
