namespace Bluepath.MapReduce
{
    using System;

    public class FileUri
    {
        private readonly string uri;

        public FileUri(string uri)
        {
            this.uri = uri;
        }

        public Uri Uri
        {
            get
            {
                return new Uri(this.uri);
            }
        }

        public override string ToString()
        {
            return this.uri;
        }

        /*public static implicit operator string(FileUri uri)
        {
            return uri.ToString();
        }

        public static implicit operator FileUri(string uri)
        {
            return new FileUri(uri);
        }*/
    }
}
