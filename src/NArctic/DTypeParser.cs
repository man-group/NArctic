using System;
using System.Collections.Generic;
using System.Text;
using Utilities;

namespace NArctic
{
	public class DTypeParser
	{
		private int Whitespace(string str,  int i)
		{
			int k = 0;
			while (i+k<str.Length && str[i+k] == ' ')
				k++;
			return k;
		}

		private int Field(string str, int i, DType cur)
		{
			int k = 0, kk;
			kk = AtChar(str, i, '(', true); i += kk; k += kk;
			if ((kk = Whitespace (str, i)) > 0) { i += kk; k += kk; }
			kk = String (str, i, out cur.Name); i += kk; k += kk;
			if ((kk = Whitespace (str, i)) > 0) { i += kk; k += kk; }
			kk = AtChar (str, i, ',', true); i += kk; k += kk;
			if ((kk = Whitespace (str, i)) > 0) { i += kk; k += kk; }
			if((kk = Parse (str, i, cur))>=0){ i += kk; k += kk; }
			kk = AtChar(str, i, ')', true); i += kk; k += kk;
			return k;
		}

		private int String(string str, int i, out string id)
		{
			id = "";
			int k = AtChar (str, i, '\'', true);
			if (k > 0) {
				i += k;
				int kk;
				while ((kk = NotAtChar (str, i, '\'')) > 0) {
					id += str [i];
					i += kk;
					k += kk;
				}
			}
			AtChar (str, i, '\'', true); i++; k++;
			return k;
		}

		private int Identifier(string str, int i, out string id)
		{
			id = "";
			int k = 0;
			for (;;) {
				int kk = AtCharRange (str, i, 'a', 'z');
				if (kk > 0) {
					id += str [i];
					k += kk;
					continue;
				}
				kk = AtCharRange (str, i, 'A', 'Z');
				if (kk > 0) {
					id += str [i];
					k += kk;
					continue;
				}
			}
		}

		private int Match (string str, int i, string pattern) {
			if (i < str.Length) {
				if (str.Substring (i, pattern.Length) == pattern)
					return pattern.Length;
				return 0;
			}
			if (pattern == null)
				return -1;
			return 0;
		}

		private void BadChar(string str, int i, string expected) {
			throw new InvalidOperationException ("expected '{0}' at pos {1} found '{2}'".Args (expected, i, str.Substring (i)));
		}

		private int AtChar(string str, int i, char c, bool ensure=false)
		{
			if (i<str.Length && str [i] == c) {
				return 1;
			}
			if (ensure) {
				BadChar (str, i, "" + c); 
			}
			if (i >= str.Length)
				return -1;
			return 0;
		}

		private int NotAtChar(string str, int i, char c, bool ensure=false)
		{
			if (i<str.Length && str [i] != c) {
				return 1;
			}
			if (ensure)
				BadChar (str, i, ""+c);
			if (i >= str.Length)
				return -1;
			return 0;
		}

		private int AtCharRange(string str, int i, char min, char max, bool ensure=false)
		{
			if (i<str.Length && str [i] >= min && str [i] <= max) {
				return 1;
			}
			if (ensure)
				BadChar (str, i, "" + min + "-" + max);
			if (i >= str.Length)
				return -1;
			return 0;
		}

		public int Parse(string str, int i, DType cur)
		{
			Whitespace (str, i);
			int k = 0, kk;
			if( (kk = AtChar(str,i,'[')) > 0) {
				cur.Type = typeof(IDictionary<string,object>);
				i += kk; k += kk;
				int nfield = 0;
				for(;;) {
					nfield++;
					if ((kk = Whitespace (str, i)) >= 0) { i += kk; k += kk; }
					if((kk = AtChar(str, i, '('))<=0)
						break;
					DType child = new DType (null, cur);
					cur.Fields.Add (child);
					kk = Field (str, i, child);
					i += kk; k += kk;
					if ((kk = Whitespace (str, i)) >= 0) { i += kk; k += kk; }
					if ((kk = AtChar (str, i, ',')) >= 0) {
						i += kk; k += kk;
						continue;
					}
				}
				if ((kk = Whitespace (str, i)) >= 0) {
					i += kk; k += kk;
				}
				kk = AtChar (str, i, ']',true);
				i += kk; k += kk;
				return k;
			}
            if ((kk = String(str, i, out string val)) > 0)
            {
                if (val.StartsWith("<"))
                    cur.Endian = EndianType.Little;
                else if(val.StartsWith(">"))
                    cur.Endian = EndianType.Big;
                else if(val.StartsWith("="))
                    cur.Endian = EndianType.Native;

                if (val == "<f8")
                {
                    cur.Type = typeof(double);
                    if (sizeof(double) != 8)
                        throw new InvalidOperationException();
                    cur.Size = 8;
                }
                else if (val == "<i8")
                {
                    cur.Type = typeof(long);
                    if (sizeof(long) != 8)
                        throw new InvalidOperationException();
                    cur.Size = 8;
                }
                else if (val == "<i4")
                {
                    cur.Type = typeof(int);
                    if (sizeof(int) != 4)
                        throw new InvalidOperationException();
                    cur.Size = 4;
                }
                else if (val == "<M8[ns]")
                {
                    cur.Type = typeof(DateTime);
                    if (sizeof(long) != 8)
                        throw new InvalidOperationException();
                    cur.Size = 8;
                }
                else if (val.ToUpper().Contains("S"))
                {
                    cur.Type = typeof(string);
                    if (!int.TryParse(val.Remove(0, 1), out int size))
                        throw new InvalidOperationException();
                    cur.Size = size;
                    cur.EncodingStyle = Encoding.UTF8;
                }
                else if (val.ToUpper().Contains("U"))
                {
                    cur.Type = typeof(string);
                    if (!int.TryParse(val.Remove(0, 2), out int size))
                        throw new InvalidOperationException();
                    cur.Size = size * 4;
                    cur.EncodingStyle = Encoding.Unicode;
                }
                else
                    throw new InvalidOperationException("unknown numpy dtype '{0}'".Args(val));
                i += kk; k += kk;
                return k;
            }
            BadChar (str, i, "\'[");
			return k;
		}
	}
}

