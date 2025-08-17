using FFMediaToolkit;
using FFMediaToolkit.Decoding;
using FFMediaToolkit.Graphics;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace InnoMedia
{
    public static class MediaUtils
    {
        /// <summary>
        /// 将 FFMediaToolkit 解码得到的图像帧数据转换为 <see cref="Texture2D"/><br/>
        /// 注意：默认格式为 RGB24 需要手动转换为 RGBA 并进行 BGR 调整
        /// </summary>
        /// <param name="graphicsDevice">用于创建 <see cref="Texture2D"/> 的图形设备</param>
        /// <param name="image">包含帧像素数据的 ImageData 对象</param>
        /// <param name="buffer">给定的原生比特数组</param>
        /// <returns>转换后的 <see cref="Texture2D"/> 图像帧</returns>
        public static unsafe Texture2D ImageDataToTexture2D(GraphicsDevice graphicsDevice, ImageData image, byte[] buffer = null) {
            int width = image.ImageSize.Width;
            int height = image.ImageSize.Height;
            byte[] rgbData = image.Data.ToArray();

            // 重用 buffer 避免每帧分配
            byte[] rgbaData = buffer ?? new byte[width * height * 4];

            fixed (byte* rgbPtr = rgbData)
            fixed (byte* rgbaPtr = rgbaData) {
                byte* src = rgbPtr;
                byte* dst = rgbaPtr;

                int pixelCount = Math.Min(rgbData.Length / 3, rgbaData.Length / 4);
                for (int i = 0; i < pixelCount; i++) {
                    dst[0] = src[2]; //R <- B
                    dst[1] = src[1]; //G <- G
                    dst[2] = src[0]; //B <- R
                    dst[3] = 255;    //A
                    src += 3;
                    dst += 4;
                }
            }

            var texture = new Texture2D(graphicsDevice, width, height, false, SurfaceFormat.Color);
            texture.SetData(rgbaData);
            return texture;
        }

        /// <summary>
        /// 将视频文件数据转化为帧列表
        /// </summary>
        /// <param name="file">视频文件数据</param>
        /// <returns></returns>
        private static IList<Texture2D> GetTexturesFromVideo(MediaFile file) {
            //预估帧数，避免List动态扩容
            int estimatedFrameCount = file.Video.Info.NumberOfFrames > 0 ? (int)file.Video.Info.NumberOfFrames : 100;
            List<Texture2D> result = new(estimatedFrameCount);

            byte[] buffer = null; //用于复用RGBA像素缓冲区

            while (file.Video.TryGetNextFrame(out var imageData)) {
                if (imageData.ImageSize.Width == 0 || imageData.ImageSize.Height == 0)
                    break;

                int width = imageData.ImageSize.Width;
                int height = imageData.ImageSize.Height;

                // 初始化复用缓冲区
                if (buffer == null || buffer.Length != width * height * 4)
                    buffer = new byte[width * height * 4];

                byte[] rgbData = imageData.Data.ToArray();

                unsafe {
                    fixed (byte* srcPtr = rgbData)
                    fixed (byte* dstPtr = buffer) {
                        byte* src = srcPtr;
                        byte* dst = dstPtr;

                        int pixelCount = Math.Min(rgbData.Length / 3, buffer.Length / 4);
                        for (int i = 0; i < pixelCount; i++) {
                            dst[0] = src[2]; //R <- B
                            dst[1] = src[1]; //G <- G
                            dst[2] = src[0]; //B <- R
                            dst[3] = 255;    //A
                            src += 3;
                            dst += 4;
                        }
                    }
                }

                var texture = new Texture2D(Main.instance.GraphicsDevice, width, height, false, SurfaceFormat.Color);
                texture.SetData(buffer);
                result.Add(texture);
            }

            return result;
        }

        /// <summary>
        /// 从视频文件路径中提取所有视频帧并转换为 <see cref="Texture2D"/> 列表
        /// </summary>
        /// <param name="path">视频文件的绝对路径</param>
        /// <returns>所有帧对应的 <see cref="Texture2D"/> 列表</returns>
        public static IList<Texture2D> GetTexturesFromVideo(string path) => GetTexturesFromVideo(MediaFile.Open(path));

        /// <summary>
        /// 从视频流中提取所有视频帧并转换为 <see cref="Texture2D"/> 列表<br/>
        /// 通常用于读取嵌入式资源或内存视频流
        /// </summary>
        /// <param name="stream">包含视频数据的 Stream 流</param>
        /// <returns>所有帧对应的 <see cref="Texture2D"/> 列表</returns>
        public static IList<Texture2D> GetTexturesFromVideo(Stream stream) => GetTexturesFromVideo(MediaFile.Open(stream));

        /// <summary>
        /// 将模组中内置的视频数据流写入临时路径用于 FFMediaToolkit 读取<br/>
        /// 文件路径为 SaveVideos/mod名称/生成文件名
        /// </summary>
        /// <param name="mod">当前模组实例</param>
        /// <param name="videoStream">待写入的视频数据流</param>
        /// <param name="fileName">可选的目标文件名 若为空将使用哈希自动生成</param>
        /// <returns>生成的临时视频文件路径</returns>
        public static string WriteModVideo(Mod mod, Stream videoStream, string fileName = "") {
            if (fileName == string.Empty) {
                fileName = videoStream.GetHashCode().ToString() + "_VideoFile";
            }
            string tempPath = Path.Combine(Main.SavePath, "SaveVideos", mod.Name, fileName);
            using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write)) {
                videoStream.CopyTo(fs);
            }
            return tempPath;
        }
    }
}
