﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BundleUtilities;

namespace BurnoutImage
{
	public struct ImageInfo
	{
		public readonly byte[] Header;
		public readonly byte[] Data;

		public ImageInfo(byte[] header, byte[] data)
		{
			this.Header = header;
			this.Data = data;
		}
	}

	public class ImageHeader
	{
		public readonly CompressionType CompressionType;
		public readonly int Width, Height;

		public ImageHeader(CompressionType compression, int width, int height)
		{
			CompressionType = compression;
			Width = width;
			Height = height;
		}
	}

	public static class GameImage
    {
        public static ImageInfo SetImage(Image newImage, CompressionType compression)
        {
            int width = newImage.Width;
            int height = newImage.Height;
			byte[] header = null;
			byte[] data = null;

			if (compression == CompressionType.BGRA)
			{
				Bitmap image = new Bitmap(newImage);
				MemoryStream mspixels = new MemoryStream();

				for (int i = 0; i < height; i++)
				{
					for (int j = 0; j < width; j++)
					{
						Color pixel = image.GetPixel(j, i);
						mspixels.WriteByte(pixel.B);
						mspixels.WriteByte(pixel.G);
						mspixels.WriteByte(pixel.R);
						mspixels.WriteByte(pixel.A);
					}
				}

				data = mspixels.ToArray();

				MemoryStream msx = new MemoryStream();
				BinaryWriter bw = new BinaryWriter(msx);

				bw.Write((int)0);
				bw.Write((int)0);
				bw.Write((int)0);
				bw.Write((int)1);

				bw.Write((int)0x15);
				bw.Write((short)width);
				bw.Write((short)height);

				bw.Write((int)0x15);
				bw.Write((int)0);

				bw.Flush();

				header = msx.ToArray();

				bw.Close();
			}
			else
			{

				DXTCompression dxt = DXTCompression.DXT1;
				if (compression == CompressionType.DXT3)
					dxt = DXTCompression.DXT1;
				else if (compression == CompressionType.DXT5)
					dxt = DXTCompression.DXT5;
				data = ImageUtil.CompressImage(newImage, dxt);

				MemoryStream msx = new MemoryStream();
				BinaryWriter bw = new BinaryWriter(msx);

				bw.Write((int)0);
				bw.Write((int)0);
				bw.Write((int)0);
				bw.Write((int)1);

				bw.Write(Encoding.ASCII.GetBytes(compression.ToString()));
				bw.Write((short)width);
				bw.Write((short)height);
				bw.Write((int)0x15);
				bw.Write((int)0);

				bw.Flush();

				header = msx.ToArray();

				bw.Close();
			}

            return new ImageInfo(header, data);
        }

		public static ImageHeader GetImageHeader(byte[] data)
		{
			try
			{
				MemoryStream ms = new MemoryStream(data);
				BinaryReader br = new BinaryReader(ms);
				if (data.Length == 0x40 || data.Length == 0x30)
				{
					// Remaster
					br.BaseStream.Seek(8, SeekOrigin.Begin);
					uint unk1 = br.ReadUInt32();
					uint unk2 = br.ReadUInt32();

					br.BaseStream.Seek(0x1C, SeekOrigin.Begin);

					CompressionType type = CompressionType.UNKNOWN;
					byte[] compression = br.ReadBytes(4);
					string compressionString = Encoding.ASCII.GetString(compression);
					if (compression.Matches(new byte[] { 0x15, 0x00, 0x00, 0x00 }))
					{
						type = CompressionType.BGRA;
					}
					else if (compression.Matches(new byte[] { 0x1C, 0x00, 0x00, 0x00 }))
					{
						type = CompressionType.RGBA;
					}
					else if (compression.Matches(new byte[] { 0xFF, 0x00, 0x00, 0x00 }))
					{
						type = CompressionType.ARGB;
					}
					else if (compression.Matches(new byte[] { 0x47, 0x00, 0x00, 0x00 }))
					{
						type = CompressionType.DXT1;
					}
					else if (compression.Matches(new byte[] { 0x4A, 0x00, 0x00, 0x00 }))
					{
						type = CompressionType.DXT3;
					}
					else if (compression.Matches(new byte[] { 0x4D, 0x00, 0x00, 0x00 }))
					{
						type = CompressionType.DXT5;
					}

					uint unk3 = br.ReadUInt32();

					int width = br.ReadInt16();
					int height = br.ReadInt16();
					br.Close();

					return new ImageHeader(type, width, height);
				}
				else
				{
					// OLD PC
					br.BaseStream.Seek(0x10, SeekOrigin.Begin);
					CompressionType type = CompressionType.UNKNOWN;
					byte[] compression = br.ReadBytes(4);
					string compressionString = Encoding.ASCII.GetString(compression);
					if (compression.Matches(new byte[] { 0x15, 0x00, 0x00, 0x00 }))
					{
						type = CompressionType.BGRA;
					}
					else if (compression.Matches(new byte[] { 0xFF, 0x00, 0x00, 0x00 }))
					{
						type = CompressionType.ARGB;
					}
					else if (compressionString.StartsWith("DXT"))
					{
						switch (compressionString[3])
						{
							case '1':
								type = CompressionType.DXT1;
								break;
							case '3':
								type = CompressionType.DXT3;
								break;
							case '5':
								type = CompressionType.DXT5;
								break;
						}
					}

					int width = br.ReadInt16();
					int height = br.ReadInt16();
					br.Close();

					return new ImageHeader(type, width, height);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
			}

			return null;
		}

	    public static Texture GetImage(byte[] data, byte[] extraData)
	    {
		    try
			{
				ImageHeader header = GetImageHeader(data);
				byte[] pixels = extraData;

				if (header.CompressionType == CompressionType.DXT1)
				{
					pixels = ImageUtil.DecompressImage(pixels, header.Width, header.Height, DXTCompression.DXT1);
				}
				else if (header.CompressionType == CompressionType.DXT3)
				{
					pixels = ImageUtil.DecompressImage(pixels, header.Width, header.Height, DXTCompression.DXT3);
				}
				else if (header.CompressionType == CompressionType.DXT5)
				{
					pixels = ImageUtil.DecompressImage(pixels, header.Width, header.Height, DXTCompression.DXT5);
				}

				//DirectBitmap bitmap = new DirectBitmap(header.Width, header.Height);

				byte[] imageData = new byte[header.Width * header.Height * 4];

				int index = 0;
				for (int y = 0; y < header.Height; y++)
				{
					for (int x = 0; x < header.Width; x++)
					{
						byte red;
						byte green;
						byte blue;
						byte alpha;
						if (header.CompressionType == CompressionType.BGRA)
						{
							blue = pixels[index + 0];
							green = pixels[index + 1];
							red = pixels[index + 2];
							alpha = pixels[index + 3];
						}
						else if (header.CompressionType == CompressionType.RGBA)
						{
							red = pixels[index + 0];
							green = pixels[index + 1];
							blue = pixels[index + 2];
							alpha = pixels[index + 3];
						} else 
						{
							alpha = pixels[index + 0];
							red = pixels[index + 1];
							green = pixels[index + 2];
							blue = pixels[index + 3];
						}

						//Color color = Color.FromArgb(alpha, red, green, blue);
						//bitmap.Bits[x + y * header.Width] = color.ToArgb();
						imageData[(x + y * header.Width) + 3] = alpha;
						imageData[(x + y * header.Width) + 2] = red;
						imageData[(x + y * header.Width) + 1] = green;
						imageData[(x + y * header.Width) + 0] = blue;
						index += 4;
					}
				}

				//return bitmap.Bitmap;

				return new Texture(imageData, header.Width, header.Height);
		    }
		    catch (Exception ex)
		    {
			    MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
			    return null;
		    }
	    }

	    public static Texture GetImagePS3(byte[] data, byte[] extraData)
        {
            if (extraData != null && data.Length == 48)
            {
                try
                {
                    MemoryStream ms = new MemoryStream(data);
                    BinaryReader br = new BinaryReader(ms);

                    byte compression = br.ReadByte();
                    byte[] unknown1 = br.ReadBytes(3);
                    CompressionType type = CompressionType.UNKNOWN;
                    if (compression == 0x85)
                    {
                        type = CompressionType.ARGB;
                    }
                    else if (compression == 0x86)
                    {
                        type = CompressionType.DXT1;
                    }
                    else if (compression == 0x88)
                    {
                        type = CompressionType.DXT5;
                    }
                    int unknown2 = Util.ReverseBytes(br.ReadInt32());
                    int width = Util.ReverseBytes(br.ReadInt16());
                    int height = Util.ReverseBytes(br.ReadInt16());
                    
                    br.Close();

                    byte[] pixels = extraData;

                    if (type == CompressionType.DXT1)
                    {
                        pixels = ImageUtil.DecompressImage(pixels, width, height, DXTCompression.DXT1);
                    }
                    else if (type == CompressionType.DXT3)
                    {
                        pixels = ImageUtil.DecompressImage(pixels, width, height, DXTCompression.DXT3);
                    }
                    else if (type == CompressionType.DXT5)
                    {
                        pixels = ImageUtil.DecompressImage(pixels, width, height, DXTCompression.DXT5);
                    }

					//DirectBitmap bitmap = new DirectBitmap(width, height);
					byte[] imageData = new byte[width * height * 4];

					int index = 0;
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            byte red;
                            byte green;
                            byte blue;
                            byte alpha;
                            if (type == CompressionType.BGRA)
                            {
                                blue = pixels[index + 0];
                                green = pixels[index + 1];
                                red = pixels[index + 2];
                                alpha = pixels[index + 3];
                            }
                            else
                            {

                                alpha = pixels[index + 0];
                                red = pixels[index + 1];
                                green = pixels[index + 2];
                                blue = pixels[index + 3];
                            }

							//Color color = Color.FromArgb(alpha, red, green, blue);
							//bitmap.Bits[x + y * width] = color.ToArgb();
							//bitmap.SetPixel(x, y, color);
							imageData[(x + y * width) + 0] = alpha;
							imageData[(x + y * width) + 1] = red;
							imageData[(x + y * width) + 2] = green;
							imageData[(x + y * width) + 3] = blue;
							index += 4;
                        }
                    }

					//return bitmap.Bitmap;

					return new Texture(imageData, width, height);
				}
                catch
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
	}

	public enum CompressionType
	{
		RGBA,
		ARGB,
		BGRA,
		DXT1,
		DXT3,
		DXT5,
		UNKNOWN
	}
}
