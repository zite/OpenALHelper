
using System;
using System.IO;
using OpenTK.Audio.OpenAL;
using OpenTK.Audio;
using System.Collections.Generic;
using System.Linq;

namespace soundtest
{
	/// <summary>
	/// The data section in the Audio Description chunk describes the format of the audio data contained within the Audio Data chunk.
	/// </summary>
	public class CAFAudioFormat
	{
		/// <summary>
		/// The number of sample frames per second of the data. You can combine this value with the frames per packet to determine the amount of time represented by a packet. This value must be nonzero.
		/// </summary>
		public double mSampleRate;
		
		/// <summary>
		/// A four-character code indicating the general kind of data in the stream. This value must be nonzero.
		/// </summary>
		public string mFormatID;
		
		/// <summary>
		/// Flags specific to each format. May be set to 0 to indicate no format flags.
		/// </summary>
		public int mFormatFlags;
		
		/// <summary>
		/// The number of bytes in a packet of data. For formats with a variable packet size, this field is set to 0. In that case, the file must include a Packet Table chunk “Packet Table Chunk.” Packets are always aligned to a byte boundary.
		/// </summary>
		public int mBytesPerPacket;
		
		/// <summary>
		/// The number of sample frames in each packet of data. For compressed formats, this field indicates the number of frames encoded in each packet. For formats with a variable number of frames per packet, this field is set to 0 and the file must include a Packet Table chunk “Packet Table Chunk.”
		/// </summary>
		public int mFramesPerPacket;
		
		/// <summary>
		/// The number of channels in each frame of data. This value must be nonzero.
		/// </summary>
		public int mChannelsPerFrame;
		
		/// <summary>
		/// The number of bits of sample data for each channel in a frame of data. This field must be set to 0 if the data format (for instance any compressed format) does not contain separate samples for each channel
		/// </summary>
		public int mBitsPerChannel;
		
		public CAFAudioFormat()
		{
		}
		
		public CAFAudioFormat(byte[] data)
		{
			InitWithData(data);
		}
		
		public void InitWithData(byte[] data)
		{
			BinaryReader br = new BinaryReader(new MemoryStream(data));
			mSampleRate = BitConverter.ToDouble(br.ReadBytes(8).Reverse().ToArray(), 0);
			mFormatID = new string(br.ReadChars(4));
			mFormatFlags = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);
			mBytesPerPacket = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);
			mFramesPerPacket = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);
			mChannelsPerFrame = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);
			mBitsPerChannel = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);
		}
		
		public ALFormat GetALFormat()
		{
			if (mChannelsPerFrame == 1)
			{
				if (mBitsPerChannel == 8)
					return ALFormat.Mono8;
				else
					return ALFormat.Mono16;
			}
			else if (mChannelsPerFrame == 2)
			{
				if (mBitsPerChannel == 8)
					return ALFormat.Stereo8;
				else
					return ALFormat.Stereo16;
			}
			
			return ALFormat.Stereo16;
		}
		
		public override string ToString ()
		{
			return string.Format("CAFHeader:\n\tmSampleRate = {0}\n\tmFormatID = {1}\n\tmFormatFlags = {2}\n" +
        			"\tmBytesPerPacket = {3}\n\tmFramesPerPacket = {4}\n\tmChannelsPerFrame = {5}\n\tmBitsPerChannel = {6}\n",
			                     mSampleRate, mFormatID, mFormatFlags, mBytesPerPacket, mFramesPerPacket, mChannelsPerFrame, mBitsPerChannel);
		}
	}

	public class OpenALHelper
	{
		public int Source;
		private int Buffer = -1;
		private AudioContext AC;
		private CAFAudioFormat cafInfo;
		
		private bool loop;
		public bool Loop
		{
			get {return loop;}
			set 
			{
				loop = value; 
				AL.Source(Source, ALSourceb.Looping, value);
			}
		}
		
		public OpenALHelper()
		{
			Init();
		}
		public OpenALHelper(string filename)
		{
			Init();
			Load(filename);
		}
		
		public void Play()
		{
			AL.SourcePlay(Source);
		}
		
		public void Stop()
		{
			AL.SourceStop(Source);
		}
		
		public void Pause()
		{
			AL.SourcePause(Source);
		}
		
		public void Load(string filename)
		{
			XRamExtension XRam = new XRamExtension();
			if (XRam.IsInitialized) 
				XRam.SetBufferMode(1, ref Buffer, XRamExtension.XRamStorage.Hardware); 
			
			BinaryReader br = new BinaryReader(File.OpenRead(filename));
			byte[] bytes = new byte[1];
			if (new string(br.ReadChars(4)) != "caff")
				throw new Exception("input file not caff");
			
			br.ReadBytes(4); // rest of caf file header
			cafInfo = new CAFAudioFormat();
			
			do {
				string type = new string(br.ReadChars(4));
				long size = BitConverter.ToInt64(br.ReadBytes(8).Reverse().ToArray(), 0);
				
				if (type == "data")
				{
					bytes = new byte[size];
					bytes = br.ReadBytes((int)size);
				}
				else if (type == "desc")
				{
					cafInfo.InitWithData(br.ReadBytes((int)size));
				}
				else
				{
					br.ReadBytes((int)size);
				}
			} while (bytes.Length == 1);
			
			br.Close();
			
			IntPtr ptr = System.Runtime.InteropServices.Marshal.AllocHGlobal(bytes.Length);
			System.Runtime.InteropServices.Marshal.Copy(bytes, 0, ptr, bytes.Length);
			AL.BufferData((uint)Buffer, cafInfo.GetALFormat(), ptr, bytes.Length, (int)cafInfo.mSampleRate);
			
			ALError error = AL.GetError();
			if (error != ALError.NoError)
			{
			   // respond to load error etc.
				Console.WriteLine("borked buffer load. ALError: " + error.ToString());
			}
			
			AL.Source(Source, ALSourcei.Buffer, (int)Buffer ); // attach the buffer to a source
		}
		
		private void Init()
		{
			AC = new AudioContext();
			
			ALError error = AL.GetError();
			if (error != ALError.NoError)
			{
				Console.WriteLine("borked audio context init. ALError: " + error.ToString());
			}
			
			Source = AL.GenSource();
			Buffer = AL.GenBuffer(); 
			
			error = AL.GetError();
			if (error != ALError.NoError)
			{
				Console.WriteLine("borked generation. ALError: " + error.ToString());
			}
		}
		
		public void Cleanup()
		{
			AL.DeleteSources(1, ref Source ); // free Handles
			AL.DeleteBuffers(1, ref Buffer );
			AC.Dispose();
		}
	}
}
