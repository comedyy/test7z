using System;
using SevenZip;

namespace MyApp // Note: actual namespace depends on the project name.
{
    internal class Program
    {
		[STAThread]
		static int Main(string[] args)
		{
			Encode("aaa", "bbb");
			Decode("bbb", "ccc");

			byte[] allBytes = File.ReadAllBytes("aaa");
			byte[] allBytes1 = File.ReadAllBytes("ccc");


			var crc1 = CRC.CalculateDigest(allBytes, 0, (uint)allBytes.Length);

			CRC c2 = new CRC();
			c2.Init();
			c2.Update(allBytes1, 0, (uint)allBytes1.Length);
			Console.WriteLine($"{crc1} equal crc {CRC.VerifyDigest(crc1, allBytes1, 0, (uint)allBytes1.Length)}");

			return 0;
		}

		private static void Encode(string inputName, string outputName)
        {
			using(Stream inStream =  new FileStream(inputName, FileMode.Open, FileAccess.Read))
			{
				using(FileStream outStream = new FileStream(outputName, FileMode.Create, FileAccess.Write))
				{
					var dictionary = 1 << 23;

					Int32 posStateBits = 2;
					Int32 litContextBits = 3; // for normal files
					// UInt32 litContextBits = 0; // for 32-bit data
					Int32 litPosBits = 0;
					// UInt32 litPosBits = 2; // for 32-bit data
					Int32 algorithm = 2;
					Int32 numFastBytes = 128;

					CoderPropID[] propIDs = 
					{
						CoderPropID.DictionarySize,
						CoderPropID.PosStateBits,
						CoderPropID.LitContextBits,
						CoderPropID.LitPosBits,
						CoderPropID.Algorithm,
						CoderPropID.NumFastBytes,
						CoderPropID.MatchFinder,
						CoderPropID.EndMarker
					};

					string mf = "bt4";
					bool eos = false;
					object[] properties = 
					{
						dictionary,
						posStateBits,
						litContextBits,
						litPosBits,
						algorithm,
						numFastBytes,
						mf,
						eos
					};

					SevenZip.Compression.LZMA.Encoder encoder = new SevenZip.Compression.LZMA.Encoder();
					encoder.SetCoderProperties(propIDs, properties);
					encoder.WriteCoderProperties(outStream);
					Int64 fileSize = inStream.Length;
					for (int i = 0; i < 8; i++)
						outStream.WriteByte((Byte)(fileSize >> (8 * i)));
					
					encoder.Code(inStream, outStream, -1, -1, null);
				}
			}
        }

        private static void Decode(string inputName, string outputName)
        {
			using(Stream inStream =  new FileStream(inputName, FileMode.Open, FileAccess.Read))
			{
				using(FileStream outStream = new FileStream(outputName, FileMode.Create, FileAccess.Write))
				{
					byte[] properties = new byte[5];
					if (inStream.Read(properties, 0, 5) != 5)
						throw (new Exception("input .lzma is too short"));
					SevenZip.Compression.LZMA.Decoder decoder = new SevenZip.Compression.LZMA.Decoder();
					decoder.SetDecoderProperties(properties);
					long outSize = 0;
					for (int i = 0; i < 8; i++)
					{
						int v = inStream.ReadByte();
						if (v < 0)
							throw (new Exception("Can't Read 1"));
						outSize |= ((long)(byte)v) << (8 * i);
					}
					long compressedSize = inStream.Length - inStream.Position;
					decoder.Code(inStream, outStream, compressedSize, outSize, null);
				}
			}
        }
    }
}