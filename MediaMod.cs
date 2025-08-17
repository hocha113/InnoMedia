using FFMediaToolkit;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace InnoMedia
{
	public class MediaMod : Mod
	{
        /// <summary>
        /// 模组会将FFmpeg的执行文件导出到该路径下
        /// </summary>
        public static string FFmpegPath => Path.Combine(Main.SavePath, "MediaModFFmpeg");

        //写在这里提醒自己，不要用懒加载
        public static MediaMod Instance => (MediaMod)ModLoader.GetMod("InnoMedia");

        public override void Load() {
            PrepareFFmpegDlls();
        }

        private static void PrepareFFmpegDlls() {
            Directory.CreateDirectory(FFmpegPath);

            //映射源路径和目标路径
            ExtractIfNotExists("lib/avcodec-61.dll", "avcodec-61.dll");
            ExtractIfNotExists("lib/avdevice-61.dll", "avdevice-61.dll");
            ExtractIfNotExists("lib/avfilter-10.dll", "avfilter-10.dll");
            ExtractIfNotExists("lib/avformat-61.dll", "avformat-61.dll");
            ExtractIfNotExists("lib/avutil-59.dll", "avutil-59.dll");
            ExtractIfNotExists("lib/postproc-58.dll", "postproc-58.dll");
            ExtractIfNotExists("lib/swresample-5.dll", "swresample-5.dll");
            ExtractIfNotExists("lib/swscale-8.dll", "swscale-8.dll");
            ExtractIfNotExists("lib/FFMediaToolkit.dll", "FFMediaToolkit.dll");

            //设置路径给 FFMediaToolkit
            FFmpegLoader.FFmpegPath = FFmpegPath;
        }

        private static void ExtractIfNotExists(string modPath, string fileName) {
            string outputPath = Path.Combine(FFmpegPath, fileName);
            if (File.Exists(outputPath)) {
                return;
            }

            using var input = Instance.GetFileStream(modPath);
            using var output = File.Create(outputPath);
            input.CopyTo(output);
        }
    }
}
