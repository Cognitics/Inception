
using System;
using System.IO;
using System.Text;
 
using UnityEngine;

namespace Cognitics.UnityCDB
{
    public class ConsoleRedirector : TextWriter
    {
        static public void Apply()
        {
            Console.SetOut(new ConsoleRedirector());
        }

        private StringBuilder buffer = new StringBuilder();

        public override Encoding Encoding => Encoding.Default;

        public override void Flush()
        {
            Debug.Log(buffer.ToString());
            buffer.Length = 0;
        }

        public override void Write(string value)
        {
            if ((value == null) || (value.Length == 0))
                return;
            buffer.Append(value);
            if (value[value.Length - 1] == '\n')
                Flush();
        }

        public override void Write(char value)
        {
            buffer.Append(value);
            if (value == '\n')
                Flush();
        }

        public override void Write(char[] value, int index, int count)
        {
            Write(new string(value, index, count));
        }

    }


}