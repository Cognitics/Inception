using System;
using System.IO;

namespace Parser
{
    class Program
    {
        private String filePath;
        
        
        Program(String fileName)
        {
            filePath = @"C:\svn\unity\Inception\obw_30m.cogbin"; //Hard coded the binary file for now. 
        }
        static float[,] ParseBinary(string filePath)
        {
            
            using (BinaryReader b = new BinaryReader(File.Open(filePath, FileMode.Open)))
            {
                
                int dimensionX = b.ReadUInt16();
                int dimensionY = b.ReadUInt16();
                float originX = b.ReadSingle();
                float originY = b.ReadSingle();
                float spacingX = b.ReadSingle();
                float spacingY = b.ReadSingle();
                float[,] tempArray = new float[dimensionX, dimensionY];
                long position = b.BaseStream.Position;
                long length = b.BaseStream.Length;
                
                for(int i = 0; i < dimensionX; i++)
                {
                    for(int j = 0; j < dimensionY; j++)
                    {
                        tempArray[i, j] = b.ReadSingle();
                    }
                }
                return tempArray;
            }
        }
    }
}
