using System;

namespace Cognitics
{
    public abstract class IImage
    {
        public virtual Type Type => typeof(void);
        public int Width = 0;
        public int Height = 0;
        public int Channels = 0;
    }

    public class Image<T> : IImage
    {
        public override Type Type => typeof(T);
        public T[] Data = null;

    }

}
