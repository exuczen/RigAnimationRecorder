using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DC
{
	public class DataFile
	{
		public byte[] data;

		public DataFile(string filepath)
		{
			this.data = File.ReadAllBytes(filepath);
		}

		public DataFile(byte[] data)
		{
			this.data = data;
		}

		public void Destroy()
		{
			data = null;
		}
	}
}
